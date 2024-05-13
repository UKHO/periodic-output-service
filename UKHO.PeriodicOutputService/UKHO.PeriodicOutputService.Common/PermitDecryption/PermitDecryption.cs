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
        private readonly ILogger<PermitDecryption> logger;
        private readonly IOptions<PermitConfiguration> permitConfiguration;
        private readonly IS63Crypt s63Crypt;

        public PermitDecryption(ILogger<PermitDecryption> logger, IOptions<PermitConfiguration> permitConfiguration, IS63Crypt s63Crypt)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.permitConfiguration = permitConfiguration ?? throw new ArgumentNullException(nameof(permitConfiguration));
            this.s63Crypt = s63Crypt ?? throw new ArgumentNullException(nameof(s63Crypt));
        }

        public PermitKey GetPermitKeys(string permit)
        {
            if (string.IsNullOrEmpty(permit)) return null;
            try
            {
                byte[] hardwareIds = GetHardwareIds();

                var cryptResult = s63Crypt.GetEncKeysFromPermit(permit, hardwareIds);

                if (cryptResult.Item1 != CryptResult.Ok)
                {
                    logger.LogError(EventIds.PermitDecryptionException.ToEventId(), $"Permit decryption failed with Error : {cryptResult.Item1}");
                    return null;
                }

                return new PermitKey
                {
                    ActiveKey = Convert.ToHexString(cryptResult.Item2),
                    NextKey = Convert.ToHexString(cryptResult.Item3)
                };
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.PermitDecryptionException.ToEventId(), ex, "An error occurred while decrypting the permit string.");
                return null;
            }
        }

        private byte[] GetHardwareIds()
        {
            var permitHardwareIds = permitConfiguration.Value.PermitDecryptionHardwareId.Split(',').ToList();
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
