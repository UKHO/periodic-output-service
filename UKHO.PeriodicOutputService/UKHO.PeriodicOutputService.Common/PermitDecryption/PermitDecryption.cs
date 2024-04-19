using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models;

namespace UKHO.PeriodicOutputService.Common.PermitDecryption
{
    [ExcludeFromCodeCoverage]
    public class PermitDecryption : IPermitDecryption
    {
        private readonly ILogger<PermitDecryption> _logger;
        private readonly IOptions<PermitConfiguration> _permitConfiguration;

        public PermitDecryption(ILogger<PermitDecryption> logger, IOptions<PermitConfiguration> permitConfiguration)
        {
            _permitConfiguration = permitConfiguration ?? throw new ArgumentNullException(nameof(permitConfiguration));
            _logger = logger;
        }

        public PermitKey GetPermitKeys(string permit)
        {
            if (string.IsNullOrEmpty(permit)) return null;
            try
            {
                byte[] hardwareIds = GetHardwareIds();
                byte[] firstCellKey = null;
                byte[] secondCellKey = null;

                S63Crypt.GetEncKeysFromPermit(permit, hardwareIds, ref firstCellKey, ref secondCellKey);

                var keys = new PermitKey
                {
                    ActiveKey = Convert.ToHexString(firstCellKey),
                    NextKey = Convert.ToHexString(secondCellKey)
                };
                return keys;
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.PermitDecryptionException.ToEventId(), ex, "An error occurred while decrypting the permit string.");
                return null;
            }
        }

        private byte[] GetHardwareIds()
        {
            var permitHardwareIds = _permitConfiguration.Value.PermitDecryptionHardwareId.Split(',').ToList();
            int i = 0;
            byte[] hardwareIds = new byte[6];
            foreach (string? hardwareId in permitHardwareIds)
            {
                hardwareIds[i++] = byte.Parse(hardwareId.Trim(), NumberStyles.AllowHexSpecifier);
            }

            return hardwareIds;
        }
    }
}
