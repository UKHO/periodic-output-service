﻿using FluentAssertions;
using UKHO.BESS.ConfigurationService.Validation;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using FluentValidation.TestHelper;

namespace UKHO.BESS.ConfigurationService.UnitTests.Validation
{
    [TestFixture]
    public class ConfigValidatorTests
    {
        private ConfigValidator configValidator;

        [SetUp]
        public void Setup()
        {
            configValidator = new ConfigValidator();
        }

        //Valid Config
        private static BessConfig GetBessConfig()
        {
            return new BessConfig
            {
                Name = "BES-1",
                ExchangeSetStandard = "S63",
                EncCellNames = new List<string> { "GB123456", "GB234567" },
                Frequency = "15 16 2 2 *",
                Type = "BASE",
                KeyFileType = "NONE",
                AllowedUsers = new List<string> { "User1", "User2" },
                AllowedUserGroups = new List<string> { "UG1", "UG2" },
                Tags = new List<Tag> { new() { Key = "key1", Value = "value1" }, new() { Key = "key2", Value = "value2" } },
                ReadMeSearchFilter = "ADDS",
                BatchExpiryInDays = 30,
                IsEnabled = true
            };
        }

        [Test]
        public void WhenConfigContainsValidAttributes_ThenNoValidationErrorsAreFound()
        {
            BessConfig bessConfig = GetBessConfig();

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);
            result.Errors.Count.Should().Be(0);
        }

        [Test]
        [TestCase("s63", "base", "key_XML", "adds")]
        [TestCase("s57", "change", "none", "blank")]
        [TestCase("s57", "change", "none", "query")]
        public void WhenConfigContainsCaseInsensitiveValidAttributes_ThenNoValidationErrorsAreFound(string exchangeSetStandard, string type, string keyFileType, string readMeSearchFilter)
        {
            BessConfig bessConfig = new()
            {
                Name = "BES-1",
                ExchangeSetStandard = exchangeSetStandard,
                EncCellNames = new List<string> { "GB123456", "GB234567" },
                Frequency = "15 16 2 2 *",
                Type = type,
                KeyFileType = keyFileType,
                AllowedUsers = new List<string> { "User1", "User2" },
                AllowedUserGroups = new List<string> { "UG1", "UG2" },
                Tags = new List<Tag> { new() { Key = "key1", Value = "value1" }, new() { Key = "key2", Value = "value2" } },
                ReadMeSearchFilter = readMeSearchFilter,
                BatchExpiryInDays = 30,
                IsEnabled = true
            };

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);
            result.Errors.Count.Should().Be(0);
        }

        [Test]
        public void WhenConfigContainsInvalidAttributes_ThenThrowValidationError()
        {
            var bessConfig = new BessConfig
            {
                Name = null,
                ExchangeSetStandard = null,
                EncCellNames = new List<string>(),
                Frequency = "",
                Type = "",
                KeyFileType = "",
                AllowedUsers = new List<string>(),
                AllowedUserGroups = new List<string>(),
                Tags = null,
                ReadMeSearchFilter = "",
                BatchExpiryInDays = 0,
                IsEnabled = null
            };

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Attribute is missing");

            result.ShouldHaveValidationErrorFor(x => x.ExchangeSetStandard)
                .WithErrorMessage("Attribute is missing or value is not provided");

            result.ShouldHaveValidationErrorFor(x => x.EncCellNames)
                .WithErrorMessage("Attribute is missing or value is not provided");

            result.ShouldHaveValidationErrorFor(x => x.Frequency)
                .WithErrorMessage("Attribute value is invalid");

            result.ShouldHaveValidationErrorFor(x => x.Type)
                .WithErrorMessage("Attribute value is invalid. Expected value is either BASE, CHANGE or UPDATE");

            result.ShouldHaveValidationErrorFor(x => x.KeyFileType)
                .WithErrorMessage("Attribute value is invalid. Expected value is KEY_TEXT, KEY_XML, PERMIT_XML or NONE");

            result.ShouldHaveValidationErrorFor(x => x.AllowedUsers)
                .WithErrorMessage("AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided");

            result.ShouldHaveValidationErrorFor(x => x.AllowedUserGroups)
                .WithErrorMessage("AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided");

            result.ShouldHaveValidationErrorFor(x => x.Tags)
                .WithErrorMessage("Attribute is missing or value not provided");

            result.ShouldHaveValidationErrorFor(x => x.ReadMeSearchFilter)
                .WithErrorMessage("Attribute is missing or value not provided");

            result.ShouldHaveValidationErrorFor(x => x.BatchExpiryInDays)
                .WithErrorMessage("Attribute is missing or value not provided");

            result.ShouldHaveValidationErrorFor(x => x.IsEnabled)
                .WithErrorMessage("Attribute is missing or value not provided. Expected value is either true or false");
        }

        [Test]
        [TestCase("")]
        [TestCase("Name/")]
        [TestCase("Abcde12345Abcde12345Abcde12345Abcde12345Abcde123451")]
        [TestCase(" ")]
        public void WhenConfigContainsInvalidNameAttribute_ThenThrowValidationError(string name)
        {
            BessConfig bessConfig = GetBessConfig();
            bessConfig.Name = name;

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);

            switch (name)
            {
                case "":
                case " ":
                    result.ShouldHaveValidationErrorFor(x => x.Name)
                        .WithErrorMessage("Value is not provided");
                    break;

                case "Name/":
                    result.ShouldHaveValidationErrorFor(x => x.Name)
                        .WithErrorMessage("Should not have characters \\/:*?\"<>|");
                    break;

                case "Abcde12345Abcde12345Abcde12345Abcde12345Abcde123451":
                    result.ShouldHaveValidationErrorFor(x => x.Name)
                        .WithErrorMessage("Name should be of max 50 characters length");
                    break;
            }
        }

        [Test]
        [TestCase("S631")]
        public void WhenConfigContainsInvalidExchangeSetType_ThenThrowValidationError(string exchangeSetStandard)
        {
            BessConfig bessConfig = GetBessConfig();
            bessConfig.ExchangeSetStandard = exchangeSetStandard;

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);

            switch (exchangeSetStandard)
            {
                case "S631":
                    result.ShouldHaveValidationErrorFor(x => x.ExchangeSetStandard)
                        .WithErrorMessage("Attribute value is invalid. Expected value is either s63 or s57");
                    break;
            }
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void WhenConfigContainsInvalidEncCellNames_ThenThrowValidationError(string? value)
        {
            BessConfig bessConfig = GetBessConfig();
            bessConfig.EncCellNames = new List<string> { value };

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);

            result.ShouldHaveValidationErrorFor(x => x.EncCellNames)
                .WithErrorMessage("Attribute is missing or value is not provided");
        }

        [Test]
        [TestCase("5 4 * *")]
        [TestCase("F 4 * * *")]
        public void WhenConfigContainsInvalidFrequency_ThenThrowValidationError(string frequency)
        {
            BessConfig bessConfig = GetBessConfig();
            bessConfig.Frequency = frequency;

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);

            result.ShouldHaveValidationErrorFor(x => x.Frequency)
                .WithErrorMessage("Attribute value is invalid");
        }

        [Test]
        [TestCase("Bespoke")]
        public void WhenConfigContainsInvalidType_ThenThrowValidationError(string type)
        {
            BessConfig bessConfig = GetBessConfig();
            bessConfig.Type = type;

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);

            result.ShouldHaveValidationErrorFor(x => x.Type)
                .WithErrorMessage("Attribute value is invalid. Expected value is either BASE, CHANGE or UPDATE");
        }

        [Test]
        [TestCase("KEY_JSON")]
        public void WhenConfigContainsInvalidKeyFileType_ThenThrowValidationError(string keyFileType)
        {
            BessConfig bessConfig = GetBessConfig();
            bessConfig.KeyFileType = keyFileType;

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);

            result.ShouldHaveValidationErrorFor(x => x.KeyFileType)
                .WithErrorMessage("Attribute value is invalid. Expected value is KEY_TEXT, KEY_XML, PERMIT_XML or NONE");
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void WhenConfigContainsInvalidAllowedUsersAndAllowedUsersGroups_ThenThrowValidationError(string? value)
        {
            BessConfig bessConfig = GetBessConfig();
            bessConfig.AllowedUsers = new List<string> { value };
            bessConfig.AllowedUserGroups = new List<string> { value };

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);

            result.ShouldHaveValidationErrorFor(x => x.AllowedUsers)
                .WithErrorMessage("AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided");
            result.ShouldHaveValidationErrorFor(x => x.AllowedUserGroups)
                .WithErrorMessage("AllowedUsers and AllowedUserGroups both attributes values are not provided. Either of them should be provided");
        }

        [Test]
        [TestCase(null,null)]
        [TestCase("Key1", null)]
        [TestCase(null, "value1")]
        public void WhenConfigContainsInvalidTags_ThenThrowValidationError(string? key, string? value)
        {
            BessConfig bessConfig = GetBessConfig();

            bessConfig.Tags = new List<Tag> { new() { Key = key, Value = value } };

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);

            result.ShouldHaveValidationErrorFor(x => x.Tags)
                .WithErrorMessage("Key or Value not provided");
        }

        [Test]
        [TestCase(-1)]
        public void WhenConfigContainsInvalidBatchExpiryInDays_ThenThrowValidationError(int batchExpiryInDays)
        {
            BessConfig bessConfig = GetBessConfig();
            bessConfig.BatchExpiryInDays = batchExpiryInDays;

            TestValidationResult<BessConfig> result = configValidator.TestValidate(bessConfig);

            result.ShouldHaveValidationErrorFor(x => x.BatchExpiryInDays)
                .WithErrorMessage("Expected value is natural number, i.e. number greater than 0");
        }
    }
}