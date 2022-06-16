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
    }
}
