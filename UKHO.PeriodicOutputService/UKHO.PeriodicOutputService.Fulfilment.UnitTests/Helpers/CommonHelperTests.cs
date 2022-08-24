using UKHO.PeriodicOutputService.Common.Helpers;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Helpers
{
    [TestFixture]
    public class CommonHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CheckMethodReturns_CorrectBase64EncodedCredentials()
        {
            var user1Credentials = CommonHelper.GetBase64EncodedCredentials("User1", "Password1");
            Assert.That(user1Credentials, Is.EqualTo("VXNlcjE6UGFzc3dvcmQx"));
        }

        [Test]
        public void CheckMethodReturns_CorrectExtractAccessToken()
        {
            var extractedAccessToken = CommonHelper.ExtractAccessToken("{\"token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6I1234212CJ9.VLSE9fRk1234.fd73LguLf_6VBefVQqu0nj8j3dovfUNVeqZDYGZ1234\",\"expiration\":\"2022-06-15T16:02:52Z\"}");
            Assert.That(extractedAccessToken, Is.EqualTo("eyJhbGciOiJIUzI1NiIsInR5cCI6I1234212CJ9.VLSE9fRk1234.fd73LguLf_6VBefVQqu0nj8j3dovfUNVeqZDYGZ1234"));
        }
    }
}
