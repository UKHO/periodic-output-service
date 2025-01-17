﻿using System.Text;
using Microsoft.AspNetCore.Mvc;
using UKHO.FmEssFssMock.API.Models.Request;
using UKHO.FmEssFssMock.API.Services;

namespace UKHO.FmEssFssMock.API.Controllers
{
    [ApiController]
    public class SalesCatalogueServiceController : BaseController
    {
        private readonly SalesCatalogueService salesCatalogueService;

        public Dictionary<string, string> ErrorsIdentifiers { get; set; }
        public Dictionary<string, string> ErrorsVersions { get; set; }
        public Dictionary<string, string> ErrorsSinceDateTime { get; set; }

        public SalesCatalogueServiceController(IHttpContextAccessor httpContextAccessor, SalesCatalogueService salesCatalogueService) : base(httpContextAccessor)
        {
            this.salesCatalogueService = salesCatalogueService;
            ErrorsIdentifiers = new Dictionary<string, string>
            {
                { "source", "productIds" },
                { "description", "None of the product Ids exist in the database" }
            };
            ErrorsVersions = new Dictionary<string, string>
            {
                { "source", "productVersions" },
                { "description", "None of the product Ids exist in the database" }
            };
            ErrorsSinceDateTime = new Dictionary<string, string>
            {
                { "source", "productSinceDateTime" },
                { "description", "None of the product Ids exist in the database" }
            };
        }

        [HttpGet]
        [Route("/scs/v1/productData/encs57/products")]
        public IActionResult ProductsSinceDateTime(string sinceDateTime)
        {
            if (!string.IsNullOrEmpty(sinceDateTime))
            {
                var response = salesCatalogueService.GetProductSinceDateTime(sinceDateTime);
                if (response != null)
                {
                    return Ok(response.ResponseBody);
                }
            }
            return BadRequest();
        }

        [HttpPost]
        [Route("/scs/v1/productData/encs57/products/productIdentifiers")]
        public IActionResult ProductIdentifiers(List<string> productIdentifiers)
        {
            if (productIdentifiers != null && productIdentifiers.Any())
            {
                var response = salesCatalogueService.GetProductIdentifier("productIdentifier-" + String.Join("-", productIdentifiers));
                if (response != null)
                {
                    return Ok(response.ResponseBody);
                }
            }
            return BadRequest(new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsIdentifiers });
        }

        [HttpPost]
        [Route("/scs/v1/productData/encs57/products/productVersions")]
        public IActionResult ProductVersions(List<ProductVersionRequest> productVersionRequest)
        {
            if (productVersionRequest != null && productVersionRequest.Any())
            {
                var productVersionRequestSearchText = new StringBuilder();
                bool isInitalIndex = true;
                const string NotModifiedProductName = "DE416040";
                const int NotModifiedEditionNumber = 11;
                const int NotModifiedUpdateNumber = 1;
                foreach (var item in productVersionRequest)
                {
                    //code added to handle 304 not modified scenario
                    if (item.ProductName == NotModifiedProductName && item.EditionNumber == NotModifiedEditionNumber && item.UpdateNumber == NotModifiedUpdateNumber)
                    {
                        return StatusCode(StatusCodes.Status304NotModified);
                    }
                    productVersionRequestSearchText.Append((isInitalIndex ? "" : "-") + item.ProductName + "-" + item.EditionNumber + "-" + item.UpdateNumber);
                    isInitalIndex = false;
                }
                var response = salesCatalogueService.GetProductVersion("productVersion-" + productVersionRequestSearchText.ToString());
                if (response != null)
                {
                    return Ok(response.ResponseBody);
                }
            }
            return BadRequest(new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsVersions });
        }

        [HttpGet]
        [Route("/scs/v1/productData/encs57/catalogue/essData")]
        public IActionResult GetEssData()
        {
            var response = salesCatalogueService.GetEssData();
            if (response != null)
            {
                return Ok(response.ResponseBody);
            }
            return BadRequest(new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsVersions });
        }
    }
}
