using NUnit.Framework;
using UKHO.ExchangeSetService.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    public class POSEndToEndFunctionalScenarios
    {
        public string userCredentialsBytes;
        private TestConfiguration config { get; set; }
        private POSWebJob WebJob { get; set; }

        private static readonly POSWebjobApiConfiguration POSWebJob = new TestConfiguration().POSWebJobConfig;
        HttpResponseMessage POSWebJobApiResponse;
        public string ESSBatchId = "2270F318-639C-4E64-A0C0-CADDD5F4EB05";
        bool checkfile;



        [OneTimeSetUp]
        public async Task Setup()
        {
            config = new TestConfiguration();
            string POSWebJobuserCredentialsBytes = CommonHelper.getbase64encodedcredentials(POSWebJob.userName, POSWebJob.password);
            POSWebJobApiResponse = await WebJob.POSWebJobEndPoint(POSWebJob.baseUrl, POSWebJobuserCredentialsBytes);
        }

        [Test]
        public async Task WhenMediaZipGetsDownloaded_ThenExtractZipAndGenerateISOAndSha1Files()
        {
            Assert.That((int)POSWebJobApiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {POSWebJobApiResponse.StatusCode}, instead of the expected status 202.");
            await Task.Delay(120000); //As this functionality is realated to a webjob not an endpoint , so this is required to complete the webjob execution and then proceed further.

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02";
                checkfile = FileContentHelper.CheckforFolderExist(Path.Combine(config.HomeDirectory, ESSBatchId), FolderName);
                Assert.IsTrue(checkfile, $"{FolderName} not Exist in the specified folder path : {Path.Combine(config.HomeDirectory, ESSBatchId)}");

                //validating iso files
                var ISOFile = $"M0{mediaNumber}X02.iso";
                checkfile = FileContentHelper.CheckforFileExist(Path.Combine(config.HomeDirectory, ESSBatchId), ISOFile);
                Assert.IsTrue(checkfile, $"{ISOFile} not Exist in the specified folder path : {Path.Combine(config.HomeDirectory, ESSBatchId)}");

                //validating iso.sha1 files
                var ShaFile = $"M0{mediaNumber}X02.iso.sha1";
                checkfile = FileContentHelper.CheckforFileExist(Path.Combine(config.HomeDirectory, ESSBatchId), ShaFile);
                Assert.IsTrue(checkfile, $"{ShaFile} not Exist in the specified folder path : {Path.Combine(config.HomeDirectory, ESSBatchId)}");
            }
        }

        [OneTimeTearDown]
        public void DeleteDirectory()
        {
            Directory.Delete(Path.Combine(config.HomeDirectory, ESSBatchId),true);
        }
    }
}
