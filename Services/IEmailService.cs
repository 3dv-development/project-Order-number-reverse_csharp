using ProjectOrderNumberSystem.Models;

namespace ProjectOrderNumberSystem.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// 採番完了メールを送信
        /// </summary>
        Task<bool> SendNotificationEmailAsync(Project project);
    }
}
