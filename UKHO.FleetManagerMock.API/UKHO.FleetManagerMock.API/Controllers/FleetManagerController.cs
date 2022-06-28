using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.FleetManagerMock.API.Common;
using System.Net;
using Microsoft.Extensions.Primitives;

namespace UKHO.FleetManagerMock.API.Controllers
{
    [ApiController]
    public class FleetManagerController : ControllerBase
    {
        private readonly IOptions<FleetManagerApiConfiguration> _fleetManagerApiConfiguration;
        private readonly IOptions<FileDirectoryPathConfiguration> _fileDirectoryPathConfiguration;
        private const string _subscriptionKey = "jsdkjsaldka";
        private const string _jwtUnpAuthToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";

        public FleetManagerController(IOptions<FleetManagerApiConfiguration> fleetManagerApiConfiguration, IOptions<FileDirectoryPathConfiguration> fileDirectoryPathConfiguration)
        {
            _fleetManagerApiConfiguration = fleetManagerApiConfiguration;
            _fileDirectoryPathConfiguration = fileDirectoryPathConfiguration;
        }

        [HttpGet]
        [Produces("application/json")]
        [Route("/auth/unp")]
        public IActionResult GetJwtAuthUnpToken([FromHeader(Name = "userPass")] string userPass, [FromHeader(Name = "Ocp-Apim-Subscription-Key")] string? subscriptionKey)
        {
            Dictionary<string, string> requestHeaders = new();
            foreach (KeyValuePair<string, StringValues> header in Request.Headers)
            {
                requestHeaders.Add(header.Key, header.Value);
            }
            string userName = _fleetManagerApiConfiguration.Value.UserName;
            string password = _fleetManagerApiConfiguration.Value.Password;
            string base64Credentials = GetBase64EncodedCredentials(userName, password);
            HttpResponseMessage httpResponse = new();
            string AuthToken = string.Empty;
            if(string.IsNullOrEmpty(subscriptionKey))
            {
                string? invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
                }
            }
            if(subscriptionKey == _subscriptionKey)
            {
                if(userPass == base64Credentials)
                {
                    string? authTokenResponse = "{\"token\": \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\", \"expiration\":\"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) + "\"}";
                    httpResponse.StatusCode = HttpStatusCode.OK;
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        AuthToken = authTokenResponse;
                    }
                }
                else
                {
                    string? invalidUsernamePasswordResponse = "{\"correlationId\":\"10ce5b89-67f4-4090-8364-7ba69eb7cb3d\",\"errors\":[{\"source\":\"userPass\",\"description\":\"Missing or invalid encoded User:Pass.\"}]}";
                    httpResponse.StatusCode = HttpStatusCode.BadRequest;
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<InvalidUsernamePasswordResponse>(invalidUsernamePasswordResponse));
                    }
                }
            }
            else
            {
                string? invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
                }
            }
            return Ok(JsonConvert.DeserializeObject<JwtAuthUnpToken>(AuthToken));
        }

        [HttpGet]
        [Route("/catalogues/{catalogueId}")]
        public IActionResult GetCatalogue([FromHeader(Name = "token")] string? token, [FromHeader(Name = "Ocp-Apim-Subscription-Key")] string? subscriptionkey)
        {
            Dictionary<string, string> requestHeaders = new();
            foreach(KeyValuePair<string, StringValues> header in Request.Headers)
            {
                requestHeaders.Add(header.Key, header.Value);
            }
            string path = _fileDirectoryPathConfiguration.Value.AVCSCatalogDataFilePath;
            HttpResponseMessage httpResponse = new();

            if(string.IsNullOrEmpty(subscriptionkey))
            {
                string? invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
                }
            }
            if(subscriptionkey == _subscriptionKey)
            {
                if(token == _jwtUnpAuthToken)
                {
                    XDocument doc = XDocument.Load(path);
                    Encoding encoding = Encoding.UTF8;
                    byte[] docAsBytes = encoding.GetBytes(doc.ToString());

                    if(docAsBytes != null)
                    {
                        httpResponse.StatusCode = HttpStatusCode.OK;
                        return File(docAsBytes, "application/xml", path);
                    }
                }
                else
                {
                    httpResponse.StatusCode = HttpStatusCode.Forbidden;
                    return StatusCode(403);
                }
            }
            else
            {
                string? invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
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
