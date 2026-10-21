#nullable enable
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System.Security;
using System.Security.AccessControl;

namespace FalcaoWebServer.Configuration
{
    public class FalcaoRegistryConfigurationProvider : ConfigurationProvider
    {
        private const string REGISTRY_BASE_PATH = @"SOFTWARE\Falcao\Secrets";

        public override void Load()
        {
            try
            {
                // =============================================================================
                // RESOLUÇÃO EXPLÍCITA DO REGISTRY BASEADA NO AMBIENTE CURIO
                // =============================================================================

                // ORDEM DE PRIORIDADE:
                // 1. HKCU (Current User) - DESENVOLVIMENTO
                // 2. HKLM (Local Machine) - PRODUÇÃO
                var curioEnvironment =
                            Environment.GetEnvironmentVariable("CURIO__ENVIRONMENT")
                            ?? throw new InvalidOperationException("CURIO__ENVIRONMENT não definida");

                RegistryKey? registryKey = null;
                string registrySource;

                if (curioEnvironment == "Development")
                {
                    registryKey = Registry.CurrentUser.OpenSubKey(
                        REGISTRY_BASE_PATH,
                        RegistryRights.ReadKey);
                    registrySource = "HKCU";
                }
                else if (curioEnvironment == "Production")
                {
                    registryKey = Registry.LocalMachine.OpenSubKey(
                        REGISTRY_BASE_PATH,
                        RegistryRights.ReadKey);
                    registrySource = "HKLM";
                }
                else
                {
                    throw new InvalidOperationException($"Ambiente CURIO inválido: {curioEnvironment}");
                }

                if (registryKey == null)
                {
                    Data["Registry:Status"] = "NOT_FOUND";
                    Data["Registry:Source"] = registrySource;
                    Data["Registry:Environment"] = curioEnvironment;
                    return;
                }

                using (registryKey)
                {
                    // FLAT DIRECT MAPPING - IGUAL AO SCRIPT POWERSHELL
                    foreach (var valueName in registryKey.GetValueNames())
                    {
                        var value = registryKey.GetValue(valueName)?.ToString();

                        if (!string.IsNullOrEmpty(value))
                        {
                            Data[valueName] = value; // NOME DIRETO DO REGISTRY (FLAT)
                        }
                    }

                    // Status
                    Data["Registry:Status"] = "LOADED";
                    Data["Registry:Source"] = registrySource;
                    Data["Registry:Path"] = $"{registrySource}\\{REGISTRY_BASE_PATH}";
                }
            }
            catch (SecurityException ex)
            {
                Data["Registry:Status"] = "ACCESS_DENIED";
                Data["Registry:Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                Data["Registry:Status"] = "ERROR";
                Data["Registry:Error"] = ex.Message;
            }
        }
    }

    public class FalcaoRegistryConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new FalcaoRegistryConfigurationProvider();
        }
    }

    public static class RegistryConfigurationExtensions
    {
        public static IConfigurationBuilder AddFalcaoRegistry(this IConfigurationBuilder builder)
        {
            return builder.Add(new FalcaoRegistryConfigurationSource());
        }
    }

    // Health Check para verificar acesso ao Registry
    public class RegistryAccessHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verificar HKCU primeiro, depois HKLM
                using var hkcuKey = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Falcao\Secrets",
                    RegistryRights.ReadKey);

                if (hkcuKey != null)
                {
                    var dbConn = hkcuKey.GetValue("DbConnection");
                    if (dbConn == null)
                        return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                            "Registry HKCU found but DbConnection missing"));

                    return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                        "Registry HKCU access OK (Development)"));
                }

                using var hklmKey = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Falcao\Secrets",
                    RegistryRights.ReadKey);

                if (hklmKey == null)
                    return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                        "Registry key not found in HKCU or HKLM"));

                var dbConnProd = hklmKey.GetValue("DbConnection");
                if (dbConnProd == null)
                    return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                        "Registry HKLM found but DbConnection missing"));

                return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    "Registry HKLM access OK (Production)"));
            }
            catch (SecurityException ex)
            {
                return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "Registry access denied", ex));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "Registry access error", ex));
            }
        }
    }
}