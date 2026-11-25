using brevo_csharp.Api;
using brevo_csharp.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Services
{
    public class BrevoEmailService
    {
        private readonly IConfiguration _config;

        public BrevoEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async System.Threading.Tasks.Task EnviarCorreoConAdjuntoAsync(
    string destinatarioPrincipal,
    string asunto,
    string mensajeHtml,
    byte[] pdfBytes,
    string nombreArchivo,
    IEnumerable<string>? correosOcultosBcc = null)
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var replyToEmail = _config["Brevo:ReplyToEmail"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
                throw new InvalidOperationException("No se cargó Brevo:ApiKey o Brevo:SenderEmail desde la configuración.");

            var config = new brevo_csharp.Client.Configuration();
            config.ApiKey["api-key"] = apiKey;

            var apiInstance = new TransactionalEmailsApi(config);

            var adjunto = new SendSmtpEmailAttachment
            {
                Content = pdfBytes,
                Name = nombreArchivo
            };

            var sender = new SendSmtpEmailSender(
                name: senderName,
                email: senderEmail
            );

            var toList = new List<SendSmtpEmailTo>
    {
        new SendSmtpEmailTo(email: destinatarioPrincipal)
    };

            var bccList = correosOcultosBcc?
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(c => !c.Equals(destinatarioPrincipal, StringComparison.OrdinalIgnoreCase))
                .Select(c => new SendSmtpEmailBcc(email: c))
                .ToList();

            var replyTo = !string.IsNullOrWhiteSpace(replyToEmail)
                ? new SendSmtpEmailReplyTo(email: replyToEmail)
                : null;

            var sendSmtpEmail = new SendSmtpEmail(
                sender: sender,
                to: toList,
                bcc: bccList is { Count: > 0 } ? bccList : null,
                subject: asunto,
                htmlContent: mensajeHtml,
                attachment: new List<SendSmtpEmailAttachment> { adjunto },
                replyTo: replyTo
            );

            await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
        }
    }
}