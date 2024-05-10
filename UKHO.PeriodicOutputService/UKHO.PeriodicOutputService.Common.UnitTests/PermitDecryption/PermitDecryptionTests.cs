using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.PermitDecryption;

namespace UKHO.PeriodicOutputService.Common.UnitTests.PermitDecryption
{
    [TestFixture]
    public class PermitDecryptionTests
    {
        private ILogger<Common.PermitDecryption.PermitDecryption> fakeLogger;
        private IOptions<PermitConfiguration> fakePermitConfiguration;
        private IS63Crypt fakeS63Crypt;
        private IPermitDecryption permitDecryption;

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<Common.PermitDecryption.PermitDecryption>>();
            fakePermitConfiguration = A.Fake<IOptions<PermitConfiguration>>();
            fakeS63Crypt = A.Fake<IS63Crypt>();

            fakePermitConfiguration.Value.PermitDecryptionHardwareId = "7E,A1,85,6E,2A";

            permitDecryption = new Common.PermitDecryption.PermitDecryption(fakeLogger, fakePermitConfiguration, fakeS63Crypt);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new Common.PermitDecryption.PermitDecryption(null, fakePermitConfiguration, fakeS63Crypt);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullPermitConfiguration = () => new Common.PermitDecryption.PermitDecryption(fakeLogger, null, fakeS63Crypt);
            nullPermitConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitConfiguration");

            Action nullS63Crypt = () => new Common.PermitDecryption.PermitDecryption(fakeLogger, fakePermitConfiguration, null);
            nullS63Crypt.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("s63Crypt");
        }

        [Test]
        public void WhenValidPermitKeyPassed_Then_GetPermitKeys_Returns_ValidData()
        {
            A.CallTo(() => fakeS63Crypt.GetEncKeysFromPermit(A<string>.Ignored, A<byte[]>.Ignored))
                                        .Returns((CryptResult.Ok, new byte[5] { 1, 2, 3, 4, 5 }, new byte[5] { 1, 2, 3, 4, 5 }));

            var result = permitDecryption.GetPermitKeys("ID123456202501016326A0EFB11E46406");

            result.ActiveKey.Should().Be("0102030405");
            result.NextKey.Should().Be("0102030405");
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void WhenNullOrEmptyPermitKeyPassed_Then_GetPermitKeys_Returns_Null(string? permitKey)
        {
            var result = permitDecryption.GetPermitKeys(permitKey);

            result.Should().BeNull();

            A.CallTo(() => fakeS63Crypt.GetEncKeysFromPermit(A<string>.Ignored, A<byte[]>.Ignored))
                                        .MustNotHaveHappened();
        }

        [Test]
        public void WhenInValidPermitKeyPassed_Then_GetPermitKeys_Returns_Null()
        {
            A.CallTo(() => fakeS63Crypt.GetEncKeysFromPermit(A<string>.Ignored, A<byte[]>.Ignored))
                                        .Returns((CryptResult.HWIDFmtErr, Array.Empty<byte>(), Array.Empty<byte>()));

            var result = permitDecryption.GetPermitKeys("ID12");

            result.Should().BeNull();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.PermitDecryptionException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit decryption failed with Error : HWIDFmtErr"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenExceptionOccurred_Then_GetPermitKeys_Returns_Null()
        {
            A.CallTo(() => fakeS63Crypt.GetEncKeysFromPermit(A<string>.Ignored, A<byte[]>.Ignored))
                                        .Throws<Exception>();

            var result = permitDecryption.GetPermitKeys("ID12");

            result.Should().BeNull();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.PermitDecryptionException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "An error occurred while decrypting the permit string."
            ).MustHaveHappenedOnceExactly();
        }

    }
}
