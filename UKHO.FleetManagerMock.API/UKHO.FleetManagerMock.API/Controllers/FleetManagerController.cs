using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UKHO.FleetManagerMock.API.Common;

namespace UKHO.FleetManagerMock.API.Controllers
{
    [ApiController]
    public class FleetManagerController : ControllerBase
    {
        private readonly IOptions<FleetManagerApiConfiguration> _fleetManagerApiConfiguration;
        private readonly IOptions<FileDirectoryPathConfiguration> _fileDirectoryPathConfiguration;

        public FleetManagerController(IOptions<FleetManagerApiConfiguration> fleetManagerApiConfiguration, IOptions<FileDirectoryPathConfiguration> fileDirectoryPathConfiguration)
        {
            _fleetManagerApiConfiguration = fleetManagerApiConfiguration;
            _fileDirectoryPathConfiguration = fileDirectoryPathConfiguration;
        }

        [HttpGet]
        [Route("/auth/unp")]
        public IActionResult GetJwtAuthUnpToken([FromHeader(Name = "userPass")] string userPass, [FromHeader(Name = "Ocp-Apim-Subscription-Key")] string subscriptionKey)
        {
            Dictionary<string, string> requestHeaders = new();
            foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in Request.Headers)
            {
                requestHeaders.Add(header.Key, header.Value);
            }

            string userName = _fleetManagerApiConfiguration.Value.UserName;
            string password = _fleetManagerApiConfiguration.Value.Password;

            string base64Credentials = GetBase64EncodedCredentials(userName, password);
            HttpResponseMessage httpResponse = new();
            string AuthToken = string.Empty;
            if (userPass == base64Credentials)
            {
               string? token = "token':'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9'";
               httpResponse.StatusCode = System.Net.HttpStatusCode.OK;

               if (httpResponse.IsSuccessStatusCode)
                {
                   AuthToken = token;
                }
            }
            else
            {
                return BadRequest(httpResponse.StatusCode);
            }
            return Ok(AuthToken);
        }

        [HttpGet]
        [Route("/catalogues/{catalogueId}")]
        public IActionResult GetCatalogue([FromHeader(Name = "token")] string token, [FromHeader(Name = "Ocp-Apim-Subscription-Key")] string subscriptionkey)
        {
            Dictionary<string, string> requestHeaders = new();
            foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in Request.Headers)
            {
                requestHeaders.Add(header.Key, header.Value);
            }
            string path = _fileDirectoryPathConfiguration.Value.AVCSCatalogDataFilePath;
            if (token == "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9")
            {
                XDocument doc = XDocument.Load(path);
                HttpResponseMessage httpResponse = new();
                Encoding encoding = Encoding.UTF8;
                byte[] docAsBytes = encoding.GetBytes(doc.ToString());

                if (docAsBytes != null)
                {
                    httpResponse.StatusCode = System.Net.HttpStatusCode.OK;
                    return File(docAsBytes, "application/xml", path);
                    
                }
            }
            return Unauthorized();
        }

        [NonAction]
        public static string GetBase64EncodedCredentials(string userName, string password)
        {
            byte[]? userCredentialsBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
            return Convert.ToBase64String(userCredentialsBytes);
        }
    }
}
