using FluentValidation;
using FluentValidation.Results;
using NCrontab.Advanced;
using NCrontab.Advanced.Enumerations;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService
{
    public interface IConfigValidator
    {
        ValidationResult Validate(BessConfig configurationSetting);
    }
    public class ConfigValidator : AbstractValidator<BessConfig>, IConfigValidator
    {
        public ConfigValidator()
        {
            RuleFor(config => config.Name).NotNull().WithMessage("Attribute is missing").
                DependentRules(() =>
                {
                    RuleFor(config => config.Name).NotEmpty().WithMessage("Value is not provided")
                     .DependentRules(() =>
                     {
                         RuleFor(config => config.Name).Length(1, 50).WithMessage("Name should be of max 50 characters length")
                    .Must(name => IsValidName(name)).WithMessage("Should not have characters \\/:*?\"<>|");
                     });
                });

            RuleFor(config => config.ExchangeSetStandard).NotNull().WithMessage("Attribute is missing or value is not provided")
                     .DependentRules(() =>
                     {
                         RuleFor(config => config.ExchangeSetStandard).Must(exchangeSetStandard => IsValidExchangeSetStandard(exchangeSetStandard.ToUpper())).WithMessage("Attribute value is invalid. Expected value is either s63 or s57");
                     });

            RuleFor(config => config.EncCellNames)
               .Must(encs => encs != null && encs.Count() > 0 && encs.All(enc => !string.IsNullOrWhiteSpace(enc)))
               .WithMessage("Attribute is missing or value is not provided");

            RuleFor(config => config.Frequency).NotNull().WithMessage("Attribute is missing or value is not provided")
                    .DependentRules(() =>
                    {
                        RuleFor(config => config.Frequency).Must(f => IsValidCron(f)).WithMessage("Attribute value is invalid.");
                    });

            RuleFor(config => config.Type).NotNull().WithMessage("Attribute is missing")
               .DependentRules(() =>
               {
                   RuleFor(config => config.Type).Must(type => IsValidBESType(type.ToUpper())).WithMessage("Attribute value is invalid. Expected value is either BASE, CHANGE or UPDATE");
               });

            RuleFor(config => config.KeyFileType).NotNull().WithMessage("Attribute is missing")
               .DependentRules(() =>
               {
                   RuleFor(config => config.KeyFileType).Must(keyFileType => IsValidKeyFileType(keyFileType.ToUpper())).WithMessage("Attribute value is invalid. Expected value is KEY_TEXT, KEY_XML, PERMIT_XML or NONE");
               });

            RuleFor(c => c.AllowedUserGroups).Must((c, s) => IsAclProvided(c)).WithMessage("AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided").When(c => c.AllowedUserGroups != null && c.AllowedUsers != null);

            RuleFor(c => c.AllowedUsers).Must((c, s) => IsAclProvided(c)).WithMessage("AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided")
            .When(c => c.AllowedUserGroups != null && c.AllowedUsers != null);

            RuleFor(config => config.Tags)
                .Must(tags => tags != null && tags.Count() > 0).WithMessage("Attribute is missing or value not provided")
                .DependentRules(() =>
                {
                    RuleFor(config => config.Tags)
                    .Must(tags => tags.All(tag => !string.IsNullOrWhiteSpace(tag?.key) && !string.IsNullOrWhiteSpace(tag?.value)))
                 .WithMessage("Key or Value not provided");
                });

            RuleFor(config => config.ReadMeSearchFilter)
                .Must(readMeSearchFilter => !string.IsNullOrWhiteSpace(readMeSearchFilter))
                .WithMessage("Attribute is missing or value not provided");

            RuleFor(b => b.BatchExpiryInDays).NotNull().WithMessage("Attribute is missing")
                .DependentRules(() =>
                {
                    RuleFor(config => config.BatchExpiryInDays).NotEmpty().WithMessage("Value is not provided")
                .DependentRules(() =>
                {
                    RuleFor(config => config.BatchExpiryInDays).GreaterThan(0).WithMessage("Expected value is natural number, i.e. number greater than 0");
                });
                });

            RuleFor(config => config.IsEnabled).NotNull().WithMessage("Attribute is missing or value is not provided")
                     .DependentRules(() =>
                     {
                         RuleFor(config => config.IsEnabled).Must(isEnabled => isEnabled == false || isEnabled == true).WithMessage("Attribute value is invalid. Expected value is either true or false");
                     });
        }
        private static bool IsAclProvided(BessConfig c)
        {
            return (c.AllowedUsers.Count() == 0 && c.AllowedUserGroups.Count() == 0) ? false : true;
        }

        static bool IsValidName(string text)
        {
            char[] SpecialChars = "\\/:*?\"<>|".ToCharArray();

            int indexOf = text.IndexOfAny(SpecialChars);
            return indexOf == -1;
        }

        private static bool IsValidBESType(string type)
        {
            return Enum.IsDefined(typeof(BESType), type);
        }

        private static bool IsValidKeyFileType(string keyFileType)
        {
            return Enum.IsDefined(typeof(KeyFileType), keyFileType);
        }

        private static bool IsValidExchangeSetStandard(string exchangeSetStandard)
        {
            return Enum.IsDefined(typeof(ExchangeSetStandard), exchangeSetStandard);
        }

        private static bool IsValidCron(string frequency)
        {
            //return CrontabSchedule.TryParse(frequency, CronStringFormat.Default) != null;
            return CrontabSchedule.TryParse(frequency, CronStringFormat.Default) == null ? false : true;
        }
        ValidationResult IConfigValidator.Validate(BessConfig bessConfig)
        {
            return Validate(bessConfig);
        }
    }
}
