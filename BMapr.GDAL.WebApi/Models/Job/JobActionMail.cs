using System.Net;
using System.Net.Mail;
using System.Xml.Linq;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionMail : JobAction
    {
        public string FromEmail { get; set; } = string.Empty;
        public string FromDisplayname { get; set; } = string.Empty;
        public string ToEmail { get; set; } = string.Empty;
        public string ToDisplayname { get; set; } = string.Empty;

        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;

        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        public List<string> FilePathAttachments { get; set; } = new List<string>();
        public List<string> Base64Attachments { get; set; } = new List<string>();


        public override Result Execute()
        {
            var result = new Result() { Succesfully = false };

            if (!Active)
            {
                result.Succesfully = true;
                result.AddMessage("Mail action is deactivated");
                return result;
            }

            var fromAddress = new MailAddress(FromEmail, FromDisplayname);
            var toAddress = new MailAddress(ToEmail, ToDisplayname);
            
            var subject = Replace(Subject);
            var body = Replace(Body);

            if (string.IsNullOrEmpty(User))
            {
                User = FromEmail;
            }

            try
            {
                var smtp = new SmtpClient
                {
                    Host = SmtpHost,
                    Port = SmtpPort,
                    EnableSsl = EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(User, Password)
                };

                var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                };

                foreach (var base64Attachment in Base64Attachments)
                {
                    var bytes = System.Convert.FromBase64String(base64Attachment);
                    var attachment = new Attachment(new MemoryStream(bytes), "Attachment");

                    message.Attachments.Add(attachment);
                }

                foreach (var filePathAttachment in FilePathAttachments)
                {
                    var filePath = Path.Combine(Config.DataProject(Project).FullName, filePathAttachment);
                    var attachment = new System.Net.Mail.Attachment(filePath);

                    message.Attachments.Add(attachment);
                }

                smtp.Send(message);

                result.Succesfully = true;
            }
            catch (Exception ex)
            {
                result.Exceptions.Add(ex);
                result.Messages.Add(ex.Message);
            }

            return result;
        }
    }
}
