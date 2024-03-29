﻿using Microsoft.AspNetCore.Mvc;
using UKHO.FmEssFssMock.API.Services;
using UKHO.FmEssFssMock.Enums;
using SystemFile = System.IO;

namespace UKHO.FmEssFssMock.API.Controllers
{
    public class MockController : Controller
    {
        private readonly string _homeDirectoryPath;
        private readonly MockService _mockService;

        public MockController(MockService mockService, IConfiguration configuration)
        {
            _mockService = mockService;

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
    }
}
