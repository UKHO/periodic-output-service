using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using UKHO.FmEssFssMock.API.Common;

namespace UKHO.FmEssFssMock.API.HealthChecks
{
    [ExcludeFromCodeCoverage]
    public class FleetManagerStubHealthCheck : IHealthCheck
    {
        private readonly IOptions<FleetManagerB2BApiConfiguration> _fleetManagerB2BApiConfiguration;

        public FleetManagerStubHealthCheck(IOptions<FleetManagerB2BApiConfiguration> fleetManagerB2BApiConfiguration)
        {
            _fleetManagerB2BApiConfiguration = fleetManagerB2BApiConfiguration;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (CheckFileContent().Status == HealthStatus.Healthy)
            {
                return Task.FromResult(
                    HealthCheckResult.Healthy("Healthy"));
            }

            return Task.FromResult(CheckFileContent());
        }

        private HealthCheckResult CheckFileContent()
        {
            string? avcsCatalogueResponseFilePath = _fleetManagerB2BApiConfiguration.Value.GetCatalogueResponseFilePath;
            string? fleetManagerStubSubscriptionKey = _fleetManagerB2BApiConfiguration.Value.SubscriptionKey;

            if (string.IsNullOrEmpty(fleetManagerStubSubscriptionKey))
            {
                const string invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.\"}";
                HealthCheckResult.Unhealthy(invalidSubscriptionKeyResponse);
            }
            if (!string.IsNullOrEmpty(fleetManagerStubSubscriptionKey))
            {
                if (!string.IsNullOrEmpty(avcsCatalogueResponseFilePath))
                {
                    Encoding encoding = Encoding.UTF8;

                    XDocument avcsCatalogDataFile = XDocument.Load(avcsCatalogueResponseFilePath);
                    byte[] avcsCatalogDataFileBytes = encoding.GetBytes(avcsCatalogDataFile.ToString());

                    if (avcsCatalogDataFileBytes != null)
                    {
                        return HealthCheckResult.Healthy();
                    }
                }
            }
            return HealthCheckResult.Unhealthy();
        }
    }
}
