using Microsoft.EntityFrameworkCore;
using ProjectOrderNumberSystem.Data;
using ProjectOrderNumberSystem.Models;
using ProjectOrderNumberSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// データベース接続文字列を環境変数またはappsettings.jsonから取得
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// 接続文字列の検証
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("[ERROR] No database connection string found. Please set DATABASE_URL environment variable or configure DefaultConnection in appsettings.json");
    throw new InvalidOperationException("Database connection string is not configured. Please set the DATABASE_URL environment variable on Render.com");
}

// RenderのPostgreSQL URLをEF Core形式に変換
if (connectionString.StartsWith("postgres://"))
{
    var originalUrl = connectionString;
    connectionString = ConvertPostgresUrl(connectionString);
    var uri = new Uri(originalUrl);
    Console.WriteLine($"[INFO] Converted PostgreSQL connection string - Host: {uri.Host}");
}
else
{
    Console.WriteLine($"[INFO] Using connection string format: {(connectionString.Contains("Host=") ? "Npgsql" : "Unknown")}");
}

// サービスの登録
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // JSONのキャメルケース (staffId) をパスカルケース (StaffId) に自動変換
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

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

// HttpContextAccessorの登録（Razorビューでセッションにアクセスするため）
builder.Services.AddHttpContextAccessor();

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
        Console.WriteLine("[INFO] Testing database connection...");
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

                // デフォルト管理者ユーザーの自動作成
                var adminEmployee = await context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == "2024");

                if (adminEmployee == null)
                {
                    Console.WriteLine("[INFO] Creating default admin user (2024)...");
                    var newAdmin = new Employee
                    {
                        EmployeeId = "2024",
                        Name = "管理者",
                        Email = "admin@3dv.co.jp",
                        IsActive = true,
                        Role = "admin"
                    };
                    context.Employees.Add(newAdmin);
                    await context.SaveChangesAsync();
                    Console.WriteLine("[INFO] Default admin user created successfully (ID: 2024, Password: 2024)");
                }
                else
                {
                    Console.WriteLine($"[INFO] Admin user 2024 already exists (Name: {adminEmployee.Name}, Role: {adminEmployee.Role})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Could not query tables: {ex.Message}");
                Console.WriteLine($"[WARNING] Inner exception: {ex.InnerException?.Message}");
            }
        }
        else
        {
            Console.WriteLine("[ERROR] Database connection failed at startup - CanConnectAsync returned false");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "データベース接続テスト中にエラーが発生しました");
        Console.WriteLine($"[ERROR] Database test exception: {ex.Message}");
        Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
            Console.WriteLine($"[ERROR] Inner exception type: {ex.InnerException.GetType().Name}");
        }
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
    try
    {
        var uri = new Uri(postgresUrl);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        Console.WriteLine($"[DEBUG] Converting connection - Host: {host}, Port: {port}, Database: {database}, User: {username}");

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to convert PostgreSQL URL: {ex.Message}");
        throw;
    }
}
