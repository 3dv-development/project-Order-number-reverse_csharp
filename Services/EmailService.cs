using System.Net;
using System.Net.Mail;
using ProjectOrderNumberSystem.Models;

namespace ProjectOrderNumberSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _env;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// 採番完了メールを送信
        /// </summary>
        public async Task<bool> SendNotificationEmailAsync(Project project)
        {
            try
            {
                var subject = $"受注番号が採番されました（受注番号: {project.ProjectNumber}）";

                var body = $@"
受注番号が採番されました。

■ 受注番号: {project.ProjectNumber}
■ カテゴリ: {project.GetCategoryName()}
■ 担当者: {project.StaffName}（{project.StaffId}）
■ 案件名: {project.ProjectName}
■ 客先名: {project.ClientName}
■ 費用: ¥{project.Budget:N0}
■ 納期: {project.Deadline:yyyy年MM月dd日}
■ 備考: {project.Remarks ?? "なし"}

採番日時: {project.CreatedAt:yyyy年MM月dd日 HH:mm}

---
3Dビジュアル 受注番号採番システム
";

                // 開発環境ではコンソール出力のみ
                if (_env.IsDevelopment())
                {
                    _logger.LogInformation("=".PadRight(60, '='));
                    _logger.LogInformation("【メール送信（開発モード）】");
                    _logger.LogInformation($"To: {_configuration["Email:NotificationEmail"]}");
                    _logger.LogInformation($"Subject: {subject}");
                    _logger.LogInformation(body);
                    _logger.LogInformation("=".PadRight(60, '='));
                    return true;
                }

                // 本番環境: メール設定チェック
                var username = _configuration["Email:Username"];
                var password = _configuration["Email:Password"];

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("メール設定が不完全なため送信をスキップしました");
                    return true;
                }

                // メール送信
                var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var defaultSender = _configuration["Email:DefaultSender"] ?? "noreply@3dv.co.jp";
                var notificationEmail = _configuration["Email:NotificationEmail"] ?? "3dvall@3dv.co.jp";

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(defaultSender),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                mailMessage.To.Add(notificationEmail);

                await client.SendMailAsync(mailMessage);

                _logger.LogInformation($"メールを送信しました: {project.ProjectNumber}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "メール送信エラー");
                // エラーが発生してもアプリケーションは継続
                return false;
            }
        }
    }
}
