using Microsoft.AspNetCore.Mvc;
using UKHO.FmEssFssMock.API.Filters;
using UKHO.FmEssFssMock.API.Models.Bess;
using UKHO.FmEssFssMock.API.Models.Response;
using UKHO.FmEssFssMock.API.Services;
using UKHO.FmEssFssMock.Enums;
using SystemFile = System.IO;

namespace UKHO.FmEssFssMock.API.Controllers
{
    public class MockController : Controller
    {
        private readonly string homeDirectoryPath;
        private readonly MockService mockService;
        private readonly AzureStorageService azureStorageService;

        public MockController(MockService mockService, IConfiguration configuration, AzureStorageService azureStorageService)
        {
            this.mockService = mockService;
            this.azureStorageService = azureStorageService;

            homeDirectoryPath = Path.Combine(Environment.CurrentDirectory, configuration["POSFolderName"]);
        }

        [HttpPost]
        [Route("/mock/configurefm/{posTestCase}")]
        public IActionResult ConfigureFleetManager(PosTestCase posTestCase)
        {
            mockService.MoveFmFolder(homeDirectoryPath);

            string sourcePath = Path.Combine(Environment.CurrentDirectory, @"Data", posTestCase.ToString(), "avcs_catalogue_ft.xml");
            string destPath = Path.Combine(homeDirectoryPath, "FM");

            if (SystemFile.File.Exists(sourcePath) && Directory.Exists(destPath))
            {
                SystemFile.File.Copy(sourcePath, Path.Combine(destPath, "avcs_catalogue_ft.xml"), true);
                mockService.UpdatePOSTestCase(posTestCase, homeDirectoryPath);
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost]
        [Route("/mock/configureAIO/{aioTestCase}")]
        public IActionResult ConfigureAioTestCase(AioTestCase aioTestCase)
        {
            mockService.UpdateAIOTestCase(aioTestCase, homeDirectoryPath);
            return Ok();
        }

        [HttpPost]
        [Route("/mock/cleanUp")]
        public IActionResult CleanUp()
        {
            bool response = mockService.CleanUp(homeDirectoryPath);
            return response ? Ok() : BadRequest();
        }

        [HttpPost]
        [ServiceFilter(typeof(SharedKeyAuthFilter))]
        [Route("/mock/bessConfigUpload")]
        public async Task<IActionResult> UploadConfigFileDataAsync([FromBody] BessConfig bessConfig)
        {
            try
            {
                if (bessConfig == null || AreAllPropertiesNull(bessConfig))
                {
                    var error = new List<Error>
                    {
                        new() { Source = "requestBody", Description = "Either body is null or malformed." }
                    };

                    return BuildBadRequestErrorResponse(error);
                }
                string result = await azureStorageService.UploadConfigurationToBlob(bessConfig);

                return !string.IsNullOrEmpty(result)
                    ? StatusCode(StatusCodes.Status201Created, result)
                    : StatusCode(StatusCodes.Status500InternalServerError, "Blob Not Created");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        protected IActionResult BuildBadRequestErrorResponse(List<Error> errors)
        {
            return new BadRequestObjectResult(errors);
        }

        private bool AreAllPropertiesNull(BessConfig bessConfig)
        {
            return bessConfig.GetType().GetProperties()
                        .Where(pi => pi.PropertyType == typeof(string))
                        .Select(pi => (string)pi.GetValue(bessConfig))
                        .Any(value => string.IsNullOrEmpty(value));
        }
    }
}
