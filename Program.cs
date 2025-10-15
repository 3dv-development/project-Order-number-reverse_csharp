using Microsoft.EntityFrameworkCore;
using ProjectOrderNumberSystem.Data;
using ProjectOrderNumberSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// データベース接続文字列を環境変数またはappsettings.jsonから取得
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// RenderのPostgreSQL URLをEF Core形式に変換
if (connectionString != null && connectionString.StartsWith("postgres://"))
{
    var originalUrl = connectionString;
    connectionString = ConvertPostgresUrl(connectionString);
    var uri = new Uri(originalUrl);
    Console.WriteLine($"[INFO] Converted PostgreSQL connection string - Host: {uri.Host}");
}
else
{
    Console.WriteLine("[WARNING] DATABASE_URL not found or not in postgres:// format");
}

// サービスの登録
builder.Services.AddControllersWithViews();

// データベースコンテキストの登録
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors();
});

// セッション設定
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Render環境用
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// HttpClientの登録（Board API用）
builder.Services.AddHttpClient();

// カスタムサービスの登録
builder.Services.AddScoped<IProjectService, ProjectService>();
// メール機能は一時的に無効化（設定未確定のため）
// builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IBoardApiService, BoardApiService>();

var app = builder.Build();

// データベース接続テスト（起動時）
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var canConnect = await context.Database.CanConnectAsync();

        if (canConnect)
        {
            Console.WriteLine("[INFO] Database connection successful");

            // テーブル存在確認
            try
            {
                var employeeCount = await context.Employees.CountAsync();
                var projectCount = await context.Projects.CountAsync();
                Console.WriteLine($"[INFO] Found {employeeCount} employees and {projectCount} projects");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Could not query tables: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("[ERROR] Database connection failed at startup");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "データベース接続テスト中にエラーが発生しました");
        Console.WriteLine($"[ERROR] Database test exception: {ex.Message}");
    }
}

// HTTPリクエストパイプラインの設定
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// PostgreSQL URL変換関数
static string ConvertPostgresUrl(string postgresUrl)
{
    var uri = new Uri(postgresUrl);
    var userInfo = uri.UserInfo.Split(':');
    var username = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}
