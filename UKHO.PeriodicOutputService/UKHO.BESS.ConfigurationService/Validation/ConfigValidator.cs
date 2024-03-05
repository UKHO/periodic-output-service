using FluentValidation;
using FluentValidation.Results;
using NCrontab.Advanced;
using NCrontab.Advanced.Enumerations;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService.Validation
{
    public interface IConfigValidator
    {
        ValidationResult Validate(BessConfig configurationSetting);
    }

    public class ConfigValidator : AbstractValidator<BessConfig>, IConfigValidator
    {
        public ConfigValidator()
        {
            RuleFor(config => config.Name)
               .Must(name => !string.IsNullOrEmpty(name?.Trim()))
               .WithMessage("Attribute is missing or value not provided")
               .DependentRules(() =>
               {
                   RuleFor(config => config.Name).Length(1, 50)
                       .WithMessage("Name should be of max 50 characters length")
                       .Must(name => IsValidName(name)).WithMessage("Should not have characters \\/:*?\"<>|");
               });

            RuleFor(config => config.ExchangeSetStandard).NotNull()
                .WithMessage("Attribute is missing or value is not provided")
                .DependentRules(() =>
                {
                    RuleFor(config => config.ExchangeSetStandard)
                        .Must(exchangeSetStandard => IsValidExchangeSetStandard(exchangeSetStandard.ToUpper()))
                        .WithMessage("Attribute value is invalid. Expected value is either s63 or s57");
                });

            RuleFor(config => config.EncCellNames)
                .Must(encs => encs != null && encs.Any() && encs.All(enc => !string.IsNullOrWhiteSpace(enc)))
                .WithMessage("Attribute is missing or value is not provided");

            RuleFor(config => config.Frequency).NotNull().WithMessage("Attribute is missing or value is not provided")
                .DependentRules(() =>
                {
                    RuleFor(config => config.Frequency).Must(f => IsValidCron(f))
                        .WithMessage("Attribute value is invalid");
                });

            RuleFor(config => config.Type).NotNull().WithMessage("Attribute is missing or value is not provided")
                .DependentRules(() =>
                {
                    RuleFor(config => config.Type).Must(type => IsValidBesType(type.ToUpper()))
                        .WithMessage("Attribute value is invalid. Expected value is either BASE, CHANGE or UPDATE");
                });

            RuleFor(config => config.KeyFileType).NotNull().WithMessage("Attribute is missing or value is not provided")
                .DependentRules(() =>
                {
                    RuleFor(config => config.KeyFileType)
                        .Must(keyFileType => IsValidKeyFileType(keyFileType.ToUpper()))
                        .WithMessage(
                            "Attribute value is invalid. Expected value is KEY_TEXT, KEY_XML, PERMIT_XML or NONE");
                });

            RuleFor(config => config.AllowedUsers).Must((config, s) => IsAclProvided(config)).WithMessage(
                "AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided");

            RuleFor(config => config.AllowedUserGroups).Must((config, s) => IsAclProvided(config)).WithMessage(
                "AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided");

            RuleFor(config => config.Tags)
                .Must(tags => tags != null && tags.Any()).WithMessage("Attribute is missing or value not provided")
                .DependentRules(() =>
                {
                    RuleFor(config => config.Tags)
                        .Must(tags => tags.All(tag =>
                            !string.IsNullOrWhiteSpace(tag?.Key) && !string.IsNullOrWhiteSpace(tag?.Value)))
                        .WithMessage("Key or Value not provided");
                });

            RuleFor(config => config.ReadMeSearchFilter)
                .Must(readMeSearchFilter => !string.IsNullOrEmpty(readMeSearchFilter?.Trim()))
                .WithMessage("Attribute is missing or value not provided");

            RuleFor(config => config.BatchExpiryInDays).NotEmpty()
                .WithMessage("Attribute is missing or value not provided")
                .DependentRules(() =>
                {
                    RuleFor(config => config.BatchExpiryInDays).GreaterThan(0)
                        .WithMessage("Expected value is natural number, i.e. number greater than 0");
                });
        }

        private static bool IsAclProvided(BessConfig c)
        {
            if ((c.AllowedUsers == null && c.AllowedUserGroups == null) ||
                    (c.AllowedUsers?.Count() == 0 && c.AllowedUserGroups?.Count() == 0) ||
                    (c.AllowedUsers == null && c.AllowedUserGroups?.Count() == 0) ||
                    (c.AllowedUsers?.Count() == 0 && c.AllowedUserGroups == null))
            {
                return false;
            }

            if (c.AllowedUsers == null && c.AllowedUserGroups != null)
            {
                if (!c.AllowedUserGroups.Any() || c.AllowedUserGroups.All(group => group?.Trim() is null or "") || c.AllowedUserGroups.Contains(string.Empty) || c.AllowedUserGroups.Contains(null))
                    return false;
            }

            if (c.AllowedUserGroups == null && c.AllowedUsers != null)
            {
                if (!c.AllowedUsers.Any() || c.AllowedUsers.All(user => user?.Trim() is null or "") || c.AllowedUsers.Contains(string.Empty) || c.AllowedUsers.Contains(null))
                    return false;
            }

            if (c.AllowedUserGroups != null && c.AllowedUsers != null)
            {
                if (c.AllowedUsers.All(user => user?.Trim() is null or "") && c.AllowedUserGroups.All(group => group?.Trim() is null or ""))
                    return false;
            }
            return true;
        }

        private static bool IsValidName(string text)
        {
            char[] specialChars = "\\/:*?\"<>|".ToCharArray();

            int indexOf = text.IndexOfAny(specialChars);
            return indexOf == -1;
        }

        private static bool IsValidBesType(string type)
        {
            return Enum.IsDefined(typeof(BessType), type);
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
            return CrontabSchedule.TryParse(frequency, CronStringFormat.Default) != null;
        }

        ValidationResult IConfigValidator.Validate(BessConfig bessConfig)
        {
            return Validate(bessConfig);
        }

        protected override bool PreValidate(ValidationContext<BessConfig> context, ValidationResult result)
        {
            if (context.InstanceToValidate.IsEnabled?.ToLower() == "no")
            {
                result.Errors.Add(new ValidationFailure("IsEnabled", "Bess config for file - " + context.InstanceToValidate.FileName + ", will be skipped for exchange set creation since the attribute value is set to “no.”"));
                return false;
            }
            else if (context.InstanceToValidate.IsEnabled?.ToLower() != "yes")
            {
                result.Errors.Add(new ValidationFailure("IsEnabled", "Attribute is missing or value not provided. Expected value is either Yes or No."));
                return false;
            }
            return true;
        }
    }
}
