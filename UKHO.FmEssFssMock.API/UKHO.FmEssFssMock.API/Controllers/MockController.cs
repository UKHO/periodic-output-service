using Microsoft.AspNetCore.Mvc;
using UKHO.FmEssFssMock.API.Services;
using UKHO.FmEssFssMock.Enums;

namespace UKHO.FmEssFssMock.API.Controllers
{
    public class MockController : Controller
    {
        private readonly string _homeDirectoryPath;
        private readonly IConfiguration _configuration;
        private readonly MockService _mockService;

        public MockController(MockService mockService, IConfiguration configuration)
        {
            _configuration = configuration;
            _mockService = mockService;

            _homeDirectoryPath = Path.Combine(_configuration["HOME"], _configuration["POSFolderName"]);
        }

        [HttpPost]
        [Route("/mock/configurefm/{posTestCase}")]
        public IActionResult ConfigureFleetManager(PosTestCase posTestCase)
        {
            string sourcePath = Path.Combine(Environment.CurrentDirectory, @"Data", posTestCase.ToString(), "avcs_catalogue_ft.xml");
            string destPath = Path.Combine(Environment.CurrentDirectory, @"Data\FM");

            if (System.IO.File.Exists(sourcePath) && Directory.Exists(destPath))
            {
                System.IO.File.Copy(sourcePath, Path.Combine(destPath, "avcs_catalogue_ft.xml"), true);
                _mockService.UpdatePOSTestCase(posTestCase);
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost]
        [Route("/mock/cleanUp")]
        public IActionResult CleanUp()
        {
            bool response = _mockService.CleanUp(_homeDirectoryPath);
            return response ? Ok() : BadRequest();
        }
    }
}
