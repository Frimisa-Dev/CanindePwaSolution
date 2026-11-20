using System.Threading.Tasks;

namespace CanindeWebServer.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Envia um e-mail simples em formato HTML.
        /// </summary>
        /// <param name="to">Destinatário</param>
        /// <param name="subject">Assunto do e-mail</param>
        /// <param name="html">Corpo em HTML</param>
        Task SendAsync(string to, string subject, string html);

        /// <summary>
        /// Envia um e-mail genérico com ou sem anexo.
        /// </summary>
        /// <param name="destinatario">E-mail de destino</param>
        /// <param name="assunto">Assunto do e-mail</param>
        /// <param name="corpo">Corpo em HTML</param>
        /// <param name="anexoPath">Caminho opcional do arquivo em anexo</param>
        Task EnviarEmailAsync(string destinatario, string assunto, string corpo, string? anexoPath = null);
    }
}