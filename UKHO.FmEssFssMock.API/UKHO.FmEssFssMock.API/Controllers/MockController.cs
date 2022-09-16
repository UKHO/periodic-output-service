using Microsoft.AspNetCore.Mvc;
using UKHO.FmEssFssMock.API.Services;
using UKHO.FmEssFssMock.Enums;
using SystemFile = System.IO;

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
            return Ok();
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
