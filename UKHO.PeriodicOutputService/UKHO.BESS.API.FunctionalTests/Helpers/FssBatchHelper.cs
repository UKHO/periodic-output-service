using Newtonsoft.Json;
using System.IO.Compression;
using UKHO.BESS.API.FunctionalTests.Models;
using static UKHO.BESS.API.FunctionalTests.Helpers.TestConfiguration;
using System.Net;
using FluentAssertions;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public static class FssBatchHelper
    {
        private static FssEndPointHelper FssApiClient { get; }
        static FssApiConfiguration config = new TestConfiguration().fssConfig;
        static BessApiConfiguration bessConfig = new TestConfiguration().bessConfig;
        static readonly TestConfiguration testConfiguration = new();

        static FssBatchHelper()
        {
            FssApiClient = new FssEndPointHelper();
        }

        /// <summary>
        /// This method is used to check the batch status.
        /// </summary>
        /// <param name="batchStatusUri"></param>
        /// <returns></returns>
        public static async Task<string> CheckBatchIsCommitted(string batchStatusUri)
        {
            string batchStatus = "";
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(config.BatchCommitWaitTime))
            {
                HttpResponseMessage batchStatusResponse = await FssApiClient.GetBatchStatusAsync(batchStatusUri);
                batchStatusResponse.StatusCode.Should().Be((HttpStatusCode)200);

                var batchStatusResponseObj = JsonConvert.DeserializeObject<ResponseBatchStatusModel>(await batchStatusResponse.Content.ReadAsStringAsync());
                batchStatus = batchStatusResponseObj!.Status!;

                if (batchStatus.Equals("Committed"))
                {
                    break;
                }
            }
            return batchStatus;
        }

        /// <summary>
        /// This method is used to extract & download the exchangeSet.
        /// </summary>
        /// <param name="downloadFileUrl"></param>
        /// <returns></returns>
        public static async Task<string> ExtractDownloadedFolder(string downloadFileUrl)
        {
            string batchId = downloadFileUrl.Split('/')[5];
            string fileName = downloadFileUrl.Split('/')[7];
            string tempFilePath = Path.Combine(Path.GetTempPath(), bessConfig.TempFolderName!);
            if (!Directory.Exists(tempFilePath))
            {
                Directory.CreateDirectory(tempFilePath);
            }

            string batchFolderPath = Path.Combine(tempFilePath, batchId);
            if (!Directory.Exists(batchFolderPath))
            {
                Directory.CreateDirectory(batchFolderPath);
            }

            var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl);
            response.StatusCode.Should().Be((HttpStatusCode)200);

            Stream stream = await response.Content.ReadAsStreamAsync();

            await using (FileStream outputFileStream = new(Path.Combine(batchFolderPath, fileName), FileMode.Create))
            {
                await stream.CopyToAsync(outputFileStream);
            }

            string zipPath = Path.Combine(batchFolderPath, fileName);
            string extractPath = Path.Combine(batchFolderPath, RenameFolder(zipPath));
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            return extractPath;
        }

        /// <summary>
        /// This method is used to rename the folder.
        /// </summary>
        /// <param name="pathInput"></param>
        /// <returns></returns>
        public static string RenameFolder(string pathInput)
        {
            string fileName = Path.GetFileName(pathInput);
            if (fileName.Contains(".zip"))
            {
                fileName = fileName.Replace(".zip", "");
            }
            return fileName;
        }

        /// <summary>
        /// This method is used to check the file existence.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool CheckForFileExist(string? filePath, string fileName)
        {
            return Directory.Exists(filePath) && File.Exists(Path.Combine(filePath, fileName));
        }

        /// <summary>
        /// This method is used to check the folder existence.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public static bool CheckForFolderExist(string filePath, string folderName)
        {
            return Directory.Exists(Path.Combine(filePath, folderName));
        }

        /// <summary>
        /// This method is used to check the existence of files and folders in the downloaded exchange set
        /// </summary>
        /// <param name="downloadFolderPath"></param>
        /// <param name="exchangeSetStandard"></param>
        /// <returns></returns>
        public static bool CheckFilesInDownloadedZip(string? downloadFolderPath, string exchangeSetStandard = "s63", bool emptyZip = false)
        {
            //Checking for the PRODUCTS.TXT file in the downloaded zip
            var checkFile = CheckForFileExist(Path.Combine(downloadFolderPath!, testConfiguration.exchangeSetDetails.ExchangeSetProductFilePath!), testConfiguration.exchangeSetDetails.ExchangeSetProductFile!);
            checkFile.Should().Be(true);

            //Checking for the README.TXT file in the downloaded zip
            checkFile = CheckForFileExist(Path.Combine(downloadFolderPath!, testConfiguration.exchangeSetDetails.ExchangeSetEncRootFolder!), testConfiguration.exchangeSetDetails.ExchangeReadMeFile!);
            checkFile.Should().Be(true);

            //Checking for the CATALOG file in the downloaded zip
            checkFile = CheckForFileExist(Path.Combine(downloadFolderPath!, testConfiguration.exchangeSetDetails.ExchangeSetEncRootFolder!), testConfiguration.exchangeSetDetails.ExchangeSetCatalogueFile!);
            checkFile.Should().Be(true);

            //Checking for the folder of the requested products in the downloaded zip
            foreach (var productName in testConfiguration.bessConfig.ProductsName!)
            {
                var countryCode = productName.Substring(0, 2);
                checkFile = CheckForFolderExist(downloadFolderPath!, "ENC_ROOT//" + countryCode + "//" + productName);
                if (emptyZip)
                {
                    checkFile.Should().Be(false);
                }
                else
                {
                    checkFile.Should().Be(true);
                }
            }

            //Checking the value of the Encryption Flag in the PRODUCTS.TXT file based on the ExchangeSet Standard
            string[] fileContent = File.ReadAllLines(Path.Combine(downloadFolderPath!, testConfiguration.exchangeSetDetails.ExchangeSetProductFilePath!, testConfiguration.exchangeSetDetails.ExchangeSetProductFile!));
            int rowNumber = new Random().Next(4, fileContent.Length - 1);
            var productData = fileContent[rowNumber].Split(",").Reverse();
            string encryptionFlag = productData.ToList()[4];
            string expectedEncryptionFlag = "1";
            if (exchangeSetStandard == "s57")
            {
                expectedEncryptionFlag = "0";
            }
            expectedEncryptionFlag.Should().Be(encryptionFlag);

            return checkFile;
        }
    }
}
