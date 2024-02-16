using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Models.BESS;

namespace UKHO.BESS.ConfigurationService
{
    public interface IConfigValidator
    {
        Task<ValidationResult> Validate(ConfigurationSetting configurationSetting);
    }
    public class ConfigValidator : AbstractValidator<ConfigurationSetting>, IConfigValidator
    {
        public ConfigValidator()
        {
            RuleFor(config => config.Name).NotNull().WithMessage("attribute is missing").
                DependentRules(() =>
                {
                    RuleFor(config => config.Name).NotEmpty().WithMessage("value is not provided")
 .DependentRules(() =>
 {
     RuleFor(config => config.Name).Length(1, 50).WithMessage("Name should be of max 50 characters length")
.Must(name => IsValidName(name)).WithMessage("should not have characters \\/:*?\"<>|");
 });
                });

            RuleFor(config => config.IsEncrypted).NotNull().WithMessage("attribute is missing or value is not provided")
                     //.DependentRules(() =>
                     //{
                     //  RuleFor(config => config.IsEncrypted).NotEmpty().WithMessage("value is not provided")
                     .DependentRules(() =>
                     {
                         RuleFor(config => config.IsEncrypted).Must(x => x == false || x == true).WithMessage(" Expected value is either true or false");
                     });
            //});

            RuleFor(config => config.IsEnabled).NotNull().WithMessage("attribute is missing or value is not provided")
                     .DependentRules(() =>
                     {
                         RuleFor(config => config.IsEnabled).Must(x => x == false || x == true).WithMessage(" Expected value is either true or false");
                     });

            RuleFor(b => b.Type).NotNull().WithMessage("attribute is missing").
                DependentRules(() =>
                {
                    RuleFor(config => config.Type).
                NotEmpty().WithMessage("value is not provided")
                .DependentRules(() =>
                {
                    RuleFor(config => config.Type).Must(type => IsValidBESType(type.ToUpper())).WithMessage("attribute value is invalid. Expected value is either BASE, CHANGE or UPDATE");
                });
                });

            RuleFor(b => b.KeyFileType).NotNull().WithMessage("attribute is missing").
               DependentRules(() =>
               {
                   RuleFor(config => config.KeyFileType).
               NotEmpty().WithMessage("value is not provided")
               .DependentRules(() =>
               {
                   RuleFor(config => config.KeyFileType).Must(keyFileType => IsValidKeyFileType(keyFileType.ToUpper())).WithMessage("attribute value is invalid. Expected value is KEY_TEXT, KEY_XML, PERMIT_XML or NONE");
               });
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

            RuleFor(b => b.ReadMeSearchFilter).NotNull().WithMessage("attribute is missing");

            RuleFor(b => b.Tags).NotNull().NotEmpty().WithMessage("attribute is missing")
                 .Must(at => at.All(a => !string.IsNullOrWhiteSpace(a.key) && !string.IsNullOrWhiteSpace(a.value)))
                 .When(ru => ru.Tags != null && ru.Tags.Count() > 0)
                 .WithMessage("key or value not provided");

            RuleFor(p => p.EncCellNames)
               .Must(pi => pi != null && pi.Count() > 0 && pi.All(u => !string.IsNullOrWhiteSpace(u)))
               .WithMessage("attribute is missing or value is not provided");

            //RuleFor(config => config.AllowedUsers.Length <1 && config.AllowedUserGroups.Length<1)
            //  .Must(users => users.All()))
            //.WithMessage("Allowed user is not in valid format.");

            RuleFor(c => c.AllowedUserGroups).Must((c, s) => IsAclProvided(c)).WithMessage("AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided").When(c => c.AllowedUserGroups != null && c.AllowedUsers != null);

            RuleFor(c => c.AllowedUsers).Must((c, s) => IsAclProvided(c)).WithMessage("AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided")
            .When(c => c.AllowedUserGroups != null && c.AllowedUsers != null);


            //RuleFor(b => b.Tags)
            //  .Must(at => at.All(a => !string.IsNullOrWhiteSpace(a.Key) && !string.IsNullOrWhiteSpace(a.Value)))
            //.WithMessage("Either key or value is not provided.");

            //RuleFor(config => config.AllowedUsers)
            //  .Must(users => users.All(user => Guid.TryParse(user, out var userOid)))
            //.WithMessage("Allowed user is not in valid format.");


        }
        private bool IsAclProvided(ConfigurationSetting c)
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
        Task<ValidationResult> IConfigValidator.Validate(ConfigurationSetting configurationSetting)
        {
            return ValidateAsync(configurationSetting);
        }

        //private bool IsValidEnum(string value, Type enumName)
        //{
        //    if (Enum.IsDefined(enumName, value))
        //        return true;
        //    return false;
        //}
    }
}
