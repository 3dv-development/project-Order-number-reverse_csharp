using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;

namespace ProjectOrderNumberSystem.Middleware
{
    /// <summary>
    /// 特定のIPアドレスからのアクセスのみを許可するミドルウェア
    /// </summary>
    public class IpRestrictionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpRestrictionMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public IpRestrictionMiddleware(
            RequestDelegate next,
            ILogger<IpRestrictionMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // IP制限が有効かどうかを確認
            var ipRestrictionEnabled = _configuration.GetValue<bool>("IpRestriction:Enabled", true);

            if (!ipRestrictionEnabled)
            {
                _logger.LogInformation("IP制限は無効化されています");
                await _next(context);
                return;
            }

            // クライアントのIPアドレスを取得
            var remoteIp = context.Connection.RemoteIpAddress;

            // プロキシ経由の場合はX-Forwarded-Forヘッダーから取得
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                if (ips.Length > 0 && IPAddress.TryParse(ips[0].Trim(), out var parsedIp))
                {
                    remoteIp = parsedIp;
                }
            }

            _logger.LogInformation($"アクセス元IP: {remoteIp}");

            // 許可されたIPアドレスのリストを取得
            var allowedIps = _configuration.GetSection("IpRestriction:AllowedIps")
                .Get<List<string>>() ?? new List<string>();

            // ローカルホスト（開発環境）からのアクセスを常に許可
            if (remoteIp != null && (
                IPAddress.IsLoopback(remoteIp) ||
                remoteIp.ToString() == "::1" ||
                remoteIp.ToString().StartsWith("127.0.0.1")))
            {
                _logger.LogInformation("ローカルホストからのアクセスを許可");
                await _next(context);
                return;
            }

            // IPアドレスが許可リストに含まれているかチェック
            if (remoteIp != null && allowedIps.Contains(remoteIp.ToString()))
            {
                _logger.LogInformation($"許可されたIPアドレスからのアクセス: {remoteIp}");
                await _next(context);
                return;
            }

            // アクセス拒否
            _logger.LogWarning($"許可されていないIPアドレスからのアクセスをブロック: {remoteIp}");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("アクセスが拒否されました。このシステムへのアクセスは許可されたIPアドレスからのみ可能です。");
        }
    }

    /// <summary>
    /// ミドルウェア登録用の拡張メソッド
    /// </summary>
    public static class IpRestrictionMiddlewareExtensions
    {
        public static IApplicationBuilder UseIpRestriction(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IpRestrictionMiddleware>();
        }
    }
}
