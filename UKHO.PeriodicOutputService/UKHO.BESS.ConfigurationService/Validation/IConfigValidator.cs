using FluentValidation.Results;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService.Validation
{
    public interface IConfigValidator
    {
        ValidationResult Validate(BessConfig configurationSetting);
    }
}
