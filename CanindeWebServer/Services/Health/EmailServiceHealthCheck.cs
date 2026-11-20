using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Sockets;

namespace CanindeWebServer.Services.Health
{
    public class EmailServiceHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;

        public EmailServiceHealthCheck(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var host = Environment.GetEnvironmentVariable("EmailHost");
                var portStr = Environment.GetEnvironmentVariable("EmailPort") ?? "587";

                if (string.IsNullOrEmpty(host))
                    return HealthCheckResult.Unhealthy("Email host não configurado");

                if (!int.TryParse(portStr, out int port))
                    port = 587;

                using var tcpClient = new TcpClient();

                var connectTask = tcpClient.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask && connectTask.IsCompletedSuccessfully)
                {
                    return HealthCheckResult.Healthy($"Email service conectável em {host}:{port}");
                }
                else
                {
                    return HealthCheckResult.Degraded($"Timeout ao conectar no email service {host}:{port}");
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Erro no email service: {ex.Message}");
            }
        }
    }
}