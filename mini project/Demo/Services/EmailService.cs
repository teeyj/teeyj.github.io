using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PuppeteerSharp;

namespace Demo.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent, string fileName)
        {
            await new BrowserFetcher().DownloadAsync();
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            using var page = await browser.NewPageAsync();

            await page.SetContentAsync(htmlContent);
            var pdfStream = await page.PdfStreamAsync(new PdfOptions { Format = PuppeteerSharp.Media.PaperFormat.A4, PrintBackground = true });

            var smtpSettings = _config.GetSection("Smtp");

            var mimeMessage = new MimeMessage();
            // From
            mimeMessage.From.Add(new MailboxAddress(smtpSettings["Name"], smtpSettings["User"]));
            // To
            mimeMessage.To.Add(MailboxAddress.Parse(toEmail));
            // Subject
            mimeMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = "<p>Here is your e-receipt for the reservation.</p>";

            using (var content = new MimeContent(pdfStream, ContentEncoding.Default))
            {
                var attachment = new MimePart("application", "pdf")
                {
                    Content = content,
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = fileName
                };

                bodyBuilder.Attachments.Add(attachment);

                mimeMessage.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(smtpSettings["Host"], int.Parse(smtpSettings["Port"]), SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpSettings["User"], smtpSettings["Pass"]);
                    await client.SendAsync(mimeMessage);
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}