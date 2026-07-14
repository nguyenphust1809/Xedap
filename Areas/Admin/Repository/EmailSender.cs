using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Xedap.Areas.Admin.Repository
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    "1150080030@gmail.com",     // Gmail gửi
                    "qthmwdrhbldiejgz"          // App Password
                ),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 10000
            };

            var mail = new MailMessage(
                "1150080030@gmail.com", // FROM phải giống Gmail gửi
                email                    // TO
            );

            mail.Subject = subject;
            mail.Body = message;
            mail.IsBodyHtml = true;

            return client.SendMailAsync(mail);
        }
    }
}
