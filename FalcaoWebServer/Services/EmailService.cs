#nullable enable
using FalcaoPwaShared.Enums;
using FalcaoWebServer.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace FalcaoWebServer.Services
{
    public class EmailService : IEmailService
    {
        private readonly FalcaoDbContext _db;
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _cfg;

        public EmailService(FalcaoDbContext db, ILogger<EmailService> logger, IConfiguration cfg)
        {
            _db = db;
            _logger = logger;
            _cfg = cfg;
        }

        private class EmailConfig
        {
            public string Host { get; set; } = "";
            public int Port { get; set; }
            public string User { get; set; } = "";
            public string Pass { get; set; } = "";
            public bool UseSsl { get; set; }
            public string From { get; set; } = "";
            public bool UseStartTls { get; set; }
        }

        // ===============================================================
        // MÉTODO CENTRALIZADO DE LEITURA DAS VARIÁVEIS DE AMBIENTE
        // ===============================================================
        private EmailConfig ObterConfiguracaoEmail()
        {
            // ✅ USAR _cfg (já carrega Registry)
            string? host = _cfg["EmailHost"];
            string portStr = _cfg["EmailPort"] ?? "587";
            string? user = _cfg["EmailUser"];
            string? pass = _cfg["EmailPassword"];
            string? from = _cfg["EmailFrom"];
            string useSslStr = _cfg["EmailUseSsl"] ?? "false";
            string useStartTlsStr = _cfg["EmailUseStartTls"] ?? "true";

            if (string.IsNullOrWhiteSpace(host))
                throw new Exception("❌ EmailHost nao configurado no Registry.");

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                throw new Exception("❌ EmailUser ou EmailPassword nao configurado no Registry.");

            if (string.IsNullOrWhiteSpace(from))
                throw new Exception("❌ EmailFrom nao configurado no Registry.");

            int port = int.TryParse(portStr, out var p) ? p : 587;
            bool useSsl = bool.TryParse(useSslStr, out var ssl) && ssl;
            bool useStartTls = bool.TryParse(useStartTlsStr, out var startTls) && startTls;

            return new EmailConfig
            {
                Host = host,
                Port = port,
                User = user,
                Pass = pass,
                UseSsl = useSsl,
                From = from,
                UseStartTls = useStartTls
            };
        }

        // ===============================================================
        // MÉTODO AUXILIAR: Determina a melhor estratégia de conexão
        // ===============================================================
        private SecureSocketOptions DeterminarOpcaoConexao(EmailConfig cfg)
        {
            // Porta 465 → SSL/TLS direto
            if (cfg.Port == 465 || cfg.UseSsl)
            {
                return SecureSocketOptions.SslOnConnect;
            }

            // Porta 587 → STARTTLS (padrão)
            if (cfg.Port == 587 || cfg.UseStartTls)
            {
                return SecureSocketOptions.StartTls;
            }

            // Porta 25 ou outros → Tenta STARTTLS se disponível
            return SecureSocketOptions.StartTlsWhenAvailable;
        }

        // ===============================================================
        // ENVIO SIMPLES (HTML)
        // ===============================================================
        public async Task SendAsync(string to, string subject, string html)
        {
            var cfg = ObterConfiguracaoEmail();

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(cfg.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            var bodyBuilder = new BodyBuilder { HtmlBody = html };
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using var smtp = new SmtpClient();

                // ✅ Usa lógica simplificada e mais robusta
                var socketOption = DeterminarOpcaoConexao(cfg);

                _logger.LogInformation("Conectando ao servidor SMTP {Host}:{Port} com {Option}",
                    cfg.Host, cfg.Port, socketOption);

                await smtp.ConnectAsync(cfg.Host, cfg.Port, socketOption);
                await smtp.AuthenticateAsync(cfg.User, cfg.Pass);
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("✅ E-mail enviado com sucesso para {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Falha ao enviar e-mail para {To}", to);
                throw;
            }
        }

        // ===============================================================
        // ENVIO DE E-MAIL COM ANEXO (relatórios, PDFs, etc.)
        // ===============================================================
        private async Task EnviarComAnexoAsync(string to, string subject, string html, string anexoPath)
        {
            var cfg = ObterConfiguracaoEmail();

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(cfg.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = html };

            if (!string.IsNullOrWhiteSpace(anexoPath) && System.IO.File.Exists(anexoPath))
            {
                builder.Attachments.Add(anexoPath);
            }

            message.Body = builder.ToMessageBody();

            try
            {
                using var smtp = new SmtpClient();

                var socketOption = DeterminarOpcaoConexao(cfg);

                _logger.LogInformation("Conectando ao servidor SMTP {Host}:{Port} com {Option}",
                    cfg.Host, cfg.Port, socketOption);

                await smtp.ConnectAsync(cfg.Host, cfg.Port, socketOption);
                await smtp.AuthenticateAsync(cfg.User, cfg.Pass);
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("✅ E-mail com anexo enviado com sucesso para {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Falha ao enviar e-mail com anexo para {To}", to);
                throw;
            }
        }

        // ===============================================================
        // Método público genérico (usa EnviarComAnexo se houver anexo)
        // ===============================================================
        public async Task EnviarEmailAsync(string destinatario, string assunto, string corpo, string? anexoPath = null)
        {
            if (string.IsNullOrWhiteSpace(anexoPath))
            {
                await SendAsync(destinatario, assunto, corpo);
            }
            else
            {
                await EnviarComAnexoAsync(destinatario, assunto, corpo, anexoPath);
            }
        }


    }
}