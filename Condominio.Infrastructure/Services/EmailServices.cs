using MailKit;
using MailKit.Net.Smtp;      // <--- ESTA ES LA CLAVE
using MailKit.Security;      // <--- ESTA ES LA CLAVE
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string mensajeHtml, byte[]? archivoAdjunto = null, string nombreArchivo = "Aviso.pdf")
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config["EmailSettings:From"]));
            email.To.Add(MailboxAddress.Parse(destinatario));
            email.Subject = asunto;

            // Usamos BodyBuilder para poder combinar HTML y archivos adjuntos
            var builder = new BodyBuilder();
            builder.HtmlBody = mensajeHtml;

            // Si hay un archivo adjunto, lo agregamos
            if (archivoAdjunto != null)
            {
                builder.Attachments.Add(nombreArchivo, archivoAdjunto, ContentType.Parse("application/pdf"));
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                await smtp.ConnectAsync(_config["EmailSettings:Host"], int.Parse(_config["EmailSettings:Port"]!), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["EmailSettings:Username"], _config["EmailSettings:Password"]);
                await smtp.SendAsync(new SendRequest(email));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando correo: {ex.Message}");
                throw;
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }


        public async Task EnviarCorreoMasivoAsync(List<string> destinatarios, string asunto, string mensajeHtml, byte[]? archivoAdjunto = null, string nombreArchivo = "Aviso.pdf")
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config["EmailSettings:From"]));

            // Ponemos a todos en BCC para que no vean los correos de los demás
            foreach (var correo in destinatarios)
            {
                email.Bcc.Add(MailboxAddress.Parse(correo));
            }

            email.Subject = asunto;

            var builder = new BodyBuilder();
            builder.HtmlBody = mensajeHtml;

            if (archivoAdjunto != null)
            {
                builder.Attachments.Add(nombreArchivo, archivoAdjunto, ContentType.Parse("application/pdf"));
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                await smtp.ConnectAsync(_config["EmailSettings:Host"], int.Parse(_config["EmailSettings:Port"]!), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["EmailSettings:Username"], _config["EmailSettings:Password"]);
                await smtp.SendAsync(new SendRequest(email));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando correo masivo: {ex.Message}");
                throw;
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}