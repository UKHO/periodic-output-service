﻿using Microsoft.AspNetCore.Mvc;
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
            try
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
            catch (Exception ex)
            {
                throw;
            }
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