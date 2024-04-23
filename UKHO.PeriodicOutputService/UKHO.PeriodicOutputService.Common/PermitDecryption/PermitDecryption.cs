using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models;

namespace UKHO.PeriodicOutputService.Common.PermitDecryption
{
    public class PermitDecryption : IPermitDecryption
    {
        private readonly ILogger<PermitDecryption> _logger;
        private readonly IOptions<PermitConfiguration> _permitConfiguration;
        private readonly IS63Crypt _s63Crypt;

        public PermitDecryption(ILogger<PermitDecryption> logger, IOptions<PermitConfiguration> permitConfiguration, IS63Crypt s63Crypt)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _permitConfiguration = permitConfiguration ?? throw new ArgumentNullException(nameof(permitConfiguration));
            _s63Crypt = s63Crypt ?? throw new ArgumentNullException(nameof(s63Crypt));
        }

        public PermitKey GetPermitKeys(string permit)
        {
            if (string.IsNullOrEmpty(permit)) return null;
            try
            {
                byte[] hardwareIds = GetHardwareIds();

                var cryptResult = _s63Crypt.GetEncKeysFromPermit(permit, hardwareIds);

                if (cryptResult.Item1 != CryptResult.Ok)
                {
                    _logger.LogError(EventIds.PermitDecryptionException.ToEventId(), "Permit decryption failed.");
                    return null;
                }

                var keys = new PermitKey
                {
                    ActiveKey = Convert.ToHexString(cryptResult.Item2),
                    NextKey = Convert.ToHexString(cryptResult.Item3)
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
