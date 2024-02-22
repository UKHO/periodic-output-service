using Microsoft.AspNetCore.Mvc;
using UKHO.FmEssFssMock.API.Models.Bess;
using UKHO.FmEssFssMock.API.Models.Response;
using UKHO.FmEssFssMock.API.Services;
using UKHO.FmEssFssMock.Enums;
using SystemFile = System.IO;

namespace UKHO.FmEssFssMock.API.Controllers
{
    public class MockController : Controller
    {
        private readonly string _homeDirectoryPath;
        private readonly MockService _mockService;
        private readonly AzureStorageService _azureStorageService;

        public MockController(MockService mockService, IConfiguration configuration, AzureStorageService azureStorageService)
        {
            _mockService = mockService;
            _azureStorageService = azureStorageService;

            _homeDirectoryPath = Path.Combine(configuration["HOME"], configuration["POSFolderName"]);
        }

        [HttpPost]
        [Route("/mock/configurefm/{posTestCase}")]
        public IActionResult ConfigureFleetManager(PosTestCase posTestCase)
        {
            _mockService.MoveFmFolder(_homeDirectoryPath);

            string sourcePath = Path.Combine(Environment.CurrentDirectory, @"Data", posTestCase.ToString(), "avcs_catalogue_ft.xml");
            string destPath = Path.Combine(_homeDirectoryPath, "FM");

            if (SystemFile.File.Exists(sourcePath) && Directory.Exists(destPath))
            {
                SystemFile.File.Copy(sourcePath, Path.Combine(destPath, "avcs_catalogue_ft.xml"), true);
                _mockService.UpdatePOSTestCase(posTestCase, _homeDirectoryPath);
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost]
        [Route("/mock/configureAIO/{aioTestCase}")]
        public IActionResult ConfigureAioTestCase(AioTestCase aioTestCase)
        {
            _mockService.UpdateAIOTestCase(aioTestCase, _homeDirectoryPath);
            return Ok();
        }

        [HttpPost]
        [Route("/mock/cleanUp")]
        public IActionResult CleanUp()
        {
            bool response = _mockService.CleanUp(_homeDirectoryPath);
            return response ? Ok() : BadRequest();
        }

        [HttpPost]
        [Route("/mock/bessConfigUpload")]
        public async Task<IActionResult> UploadConfigFileDataAsync([FromBody] List<BessConfig> bessConfigs)
        {
            try
            {
                if (bessConfigs.Any())
                {
                    string result = await _azureStorageService.UploadConfigurationToBlob(bessConfigs);

                    return !string.IsNullOrEmpty(result)
                        ? StatusCode(StatusCodes.Status201Created, result)
                        : StatusCode(StatusCodes.Status500InternalServerError, "Blob Not Created");
                }

                var error = new List<Error>
                {
                    new() { Source = "requestBody", Description = "Either body is null or malformed." }
                };

                return BuildBadRequestErrorResponse(error);
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
    }
}
