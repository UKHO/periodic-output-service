using FluentValidation;
using FluentValidation.Results;
using UKHO.FmEssFssMock.API.Models.Bess;

namespace UKHO.FmEssFssMock.API.Validation
{
    public interface IConfigValidator
    {
        ValidationResult Validate(BessConfig configurationSetting);
    }

    public class ConfigValidator : AbstractValidator<BessConfig>, IConfigValidator
    {
        public ConfigValidator()
        {
            RuleFor(config => config.Name).NotNull().WithMessage("attribute is missing").DependentRules(() =>
            {
                RuleFor(config => config.Name).NotEmpty().WithMessage("value is not provided")
                    .DependentRules(() =>
                    {
                        RuleFor(config => config.Name).Length(1, 50)
                            .WithMessage("Name should be of max 50 characters length")
                            .Must(name => IsValidName(name)).WithMessage("should not have characters \\/:*?\"<>|");
                    });
            });

            RuleFor(config => config.ExchangeSetStandard).NotNull().WithMessage("attribute is missing or value is not provided")
                     .DependentRules(() =>
                     {
                         RuleFor(config => config.ExchangeSetStandard).Must(exchangeSetStandard => IsValidExchangeSetStandard(exchangeSetStandard.ToUpper())).WithMessage("attribute value is invalid. Expected value is either s63 or s57");
                     });

            RuleFor(config => config.IsEnabled).NotNull().WithMessage("attribute is missing or value is not provided")
                     .DependentRules(() =>
                     {
                         RuleFor(config => config.IsEnabled).Must(x => x == false || x == true).WithMessage("attribute value is invalid. Expected value is either true or false");
                     });

            RuleFor(b => b.Type).NotNull().WithMessage("attribute is missing")
                .DependentRules(() =>
                {
                    RuleFor(config => config.Type).Must(type => IsValidBESType(type.ToUpper())).WithMessage("attribute value is invalid. Expected value is either BASE, CHANGE or UPDATE");
                    //});
                });

            RuleFor(b => b.KeyFileType).NotNull().WithMessage("attribute is missing")
               .DependentRules(() =>
               {
                   RuleFor(config => config.KeyFileType).Must(keyFileType => IsValidKeyFileType(keyFileType.ToUpper())).WithMessage("attribute value is invalid. Expected value is KEY_TEXT, KEY_XML, PERMIT_XML or NONE");
                   //});
               });

            RuleFor(b => b.BatchExpiryInDays).NotNull().WithMessage("attribute is missing")
                .DependentRules(() =>
                {
                    RuleFor(config => config.BatchExpiryInDays).NotEmpty().WithMessage("value is not provided")
                .DependentRules(() =>
                {
                    RuleFor(config => config.BatchExpiryInDays).GreaterThan(0).WithMessage("Expected value is natural number, i.e. number greater than 0");
                });
                });

            RuleFor(b => b.ReadMeSearchFilter)
                .Must(b => !string.IsNullOrWhiteSpace(b))
                .WithMessage("attribute is missing or value not provided");

            RuleFor(config => config.EncCellNames)
               .Must(encs => encs != null && encs.Count() > 0 && encs.All(enc => !string.IsNullOrWhiteSpace(enc)))
               .WithMessage("attribute is missing or value is not provided");

            RuleFor(config => config.Tags)//.NotNull().NotEmpty().WithMessage("attribute is missing")
                .Must(tags => tags != null && tags.Count() > 0).WithMessage("attribute is missing or value not provided")
                .DependentRules(() =>
                {
                    RuleFor(config => config.Tags)
                    .Must(tags => tags.All(tag => !string.IsNullOrWhiteSpace(tag?.Key) && !string.IsNullOrWhiteSpace(tag?.Value)))
                 .WithMessage("key or value not provided");
                });

            RuleFor(config => config.EncCellNames)
               .Must(encs => encs != null && encs.Count() > 0 && encs.All(enc => !string.IsNullOrWhiteSpace(enc)))
               .WithMessage("attribute is missing or value is not provided");

            RuleFor(c => c.AllowedUserGroups).Must((c, s) => IsAclProvided(c)).WithMessage("AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided").When(c => c.AllowedUserGroups != null && c.AllowedUsers != null);

            RuleFor(c => c.AllowedUsers).Must((c, s) => IsAclProvided(c)).WithMessage("AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided")
            .When(c => c.AllowedUserGroups != null && c.AllowedUsers != null);
        }

        private bool IsAclProvided(BessConfig c)
        {
            if (c.AllowedUsers.Count() == 0 && c.AllowedUserGroups.Count() == 0)
                return false;
            return true;
        }

        bool IsValidName(string text)
        {
            char[] SpecialChars = "\\/:*?\"<>|".ToCharArray();

            int indexOf = text.IndexOfAny(SpecialChars);
            if (indexOf == -1)
            {
                // No special chars
                return true;
            }
            return false;
        }

        private bool IsValidBESType(string type)
        {
            if (Enum.IsDefined(typeof(BESType), type))
                return true;
            return false;
        }

        private bool IsValidKeyFileType(string keyFileType)
        {
            if (Enum.IsDefined(typeof(KeyFileType), keyFileType))
                return true;
            return false;
        }

        private bool IsValidExchangeSetStandard(string exchangeSetStandard)
        {
            if (Enum.IsDefined(typeof(ExchangeSetStandard), exchangeSetStandard))
                return true;
            return false;
        }

        ValidationResult IConfigValidator.Validate(BessConfig bessConfig)
        {
            return Validate(bessConfig);
        }
    }

    public enum BESType
    {
        BASE = 1,
        UPDATE = 2,
        CHANGE = 3
    }

    public enum ExchangeSetStandard
    {
        S57 = 1,
        S63 = 2
    }

    public enum KeyFileType
    {
        KEY_TEXT = 1,
        KEY_XML = 2,
        PERMIT_XML = 3,
        NONE = 4
    }
}
