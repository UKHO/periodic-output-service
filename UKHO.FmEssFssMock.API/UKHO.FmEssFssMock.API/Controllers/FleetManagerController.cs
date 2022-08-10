using System.Globalization;
using System.Net;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.FmEssFssMock.API.Common;

namespace UKHO.FmEssFssMock.API.Controllers
{
    [ApiController]
    public class FleetManagerController : ControllerBase
    {
        private readonly IOptions<FleetManagerApiConfiguration> _fleetManagerApiConfiguration;
        private readonly IOptions<FileDirectoryPathConfiguration> _fileDirectoryPathConfiguration;
        private const string _jwtUnpAuthToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";

        public FleetManagerController(IOptions<FleetManagerApiConfiguration> fleetManagerApiConfiguration, IOptions<FileDirectoryPathConfiguration> fileDirectoryPathConfiguration)
        {
            _fleetManagerApiConfiguration = fleetManagerApiConfiguration;
            _fileDirectoryPathConfiguration = fileDirectoryPathConfiguration;
        }

        [HttpGet]
        [Produces("application/json")]
        [Route("/fm/auth/unp")]
        public IActionResult GetJwtAuthUnpToken([FromHeader(Name = "userPass")] string userPass, [FromHeader(Name = "Ocp-Apim-Subscription-Key")] string? subscriptionKey)
        {
            string? userName = _fleetManagerApiConfiguration.Value.UserName;
            string? password = _fleetManagerApiConfiguration.Value.Password;
            string? fleetManagerStubSubscriptionKey = _fleetManagerApiConfiguration.Value.SubscriptionKey;
            string base64Credentials = GetBase64EncodedCredentials(userName, password);
            HttpResponseMessage httpResponse = new();
            string AuthToken = string.Empty;
            if (string.IsNullOrEmpty(subscriptionKey))
            {
                const string invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
                }
            }
            if (subscriptionKey == fleetManagerStubSubscriptionKey)
            {
                if (userPass == base64Credentials)
                {
                    string authTokenResponse = "{\"token\": \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\", \"expiration\":\"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) + "\"}";
                    httpResponse.StatusCode = HttpStatusCode.OK;
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        AuthToken = authTokenResponse;
                    }
                }
                else
                {
                    const string invalidUsernamePasswordResponse = "{\"correlationId\":\"10ce5b89-67f4-4090-8364-7ba69eb7cb3d\",\"errors\":[{\"source\":\"userPass\",\"description\":\"Missing or invalid encoded User:Pass.\"}]}";
                    httpResponse.StatusCode = HttpStatusCode.BadRequest;
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<InvalidUsernamePasswordResponse>(invalidUsernamePasswordResponse));
                    }
                }
            }
            else
            {
                const string invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
                }
            }
            return Ok(JsonConvert.DeserializeObject<JwtAuthUnpToken>(AuthToken));
        }

        [HttpGet]
        [Route("/fm/catalogues/{catalogueId}")]
        public IActionResult GetCatalogue([FromHeader(Name = "token")] string? token, [FromHeader(Name = "Ocp-Apim-Subscription-Key")] string? subscriptionKey)
        {
            string? fleetManagerStubSubscriptionKey = _fleetManagerApiConfiguration.Value.SubscriptionKey;
            string? path = _fileDirectoryPathConfiguration.Value.AVCSCatalogDataFilePath;
            HttpResponseMessage httpResponse = new();

            if (string.IsNullOrEmpty(subscriptionKey))
            {
                const string invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
                }
            }
            if (subscriptionKey == fleetManagerStubSubscriptionKey)
            {
                if (token == _jwtUnpAuthToken && !string.IsNullOrEmpty(path))
                {
                    XDocument doc = XDocument.Load(path);
                    Encoding encoding = Encoding.UTF8;
                    byte[] docAsBytes = encoding.GetBytes(doc.ToString());

                    if (docAsBytes != null)
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
                const string invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
                }
            }

            return Unauthorized();
        }


        [HttpGet]
        [Produces("application/json")]
        [Route("/fm/ft/auth/unp")]
        public IActionResult GetJwtAuthUnpTokenForFT([FromHeader(Name = "userPass")] string userPass, [FromHeader(Name = "Ocp-Apim-Subscription-Key")] string? subscriptionKey)
        {
            string? userName = _fleetManagerApiConfiguration.Value.UserName;
            string? password = _fleetManagerApiConfiguration.Value.Password;
            string? fleetManagerStubSubscriptionKey = _fleetManagerApiConfiguration.Value.SubscriptionKey;
            string base64Credentials = GetBase64EncodedCredentials(userName, password);
            HttpResponseMessage httpResponse = new();
            string AuthToken = string.Empty;
            if (string.IsNullOrEmpty(subscriptionKey))
            {
                const string invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
                }
            }
            if (subscriptionKey == fleetManagerStubSubscriptionKey)
            {
                if (userPass == base64Credentials)
                {
                    string authTokenResponse = "{\"token\": \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\", \"expiration\":\"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) + "\"}";
                    httpResponse.StatusCode = HttpStatusCode.OK;
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        AuthToken = authTokenResponse;
                    }
                }
                else
                {
                    const string invalidUsernamePasswordResponse = "{\"correlationId\":\"10ce5b89-67f4-4090-8364-7ba69eb7cb3d\",\"errors\":[{\"source\":\"userPass\",\"description\":\"Missing or invalid encoded User:Pass.\"}]}";
                    httpResponse.StatusCode = HttpStatusCode.BadRequest;
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        return BadRequest(JsonConvert.DeserializeObject<InvalidUsernamePasswordResponse>(invalidUsernamePasswordResponse));
                    }
                }
            }
            else
            {
                const string invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
                }
            }
            return Ok(JsonConvert.DeserializeObject<JwtAuthUnpToken>(AuthToken));
        }

        [HttpGet]
        [Route("/fm/ft/catalogues/{catalogueId}")]
        public IActionResult GetCatalogueForFT([FromHeader(Name = "token")] string? token, [FromHeader(Name = "Ocp-Apim-Subscription-Key")] string? subscriptionKey)
        {
            string? fleetManagerStubSubscriptionKey = _fleetManagerApiConfiguration.Value.SubscriptionKey;
            string? path = _fileDirectoryPathConfiguration.Value.FunctionalTestAVCSCatalogDataFilePath;
            HttpResponseMessage httpResponse = new();

            if (string.IsNullOrEmpty(subscriptionKey))
            {
                const string invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
                }
            }
            if (subscriptionKey == fleetManagerStubSubscriptionKey)
            {
                if (token == _jwtUnpAuthToken && !string.IsNullOrEmpty(path))
                {
                    XDocument doc = XDocument.Load(path);
                    Encoding encoding = Encoding.UTF8;
                    byte[] docAsBytes = encoding.GetBytes(doc.ToString());

                    if (docAsBytes != null)
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
                const string invalidSubscriptionKeyResponse = "{\"statusCode\": 401, \"message\":\"Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.\"}";
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Unauthorized(JsonConvert.DeserializeObject<InvalidSubscriptionKeyResponse>(invalidSubscriptionKeyResponse));
                }
            }

            return Unauthorized();
        }
        [NonAction]
        public static string GetBase64EncodedCredentials(string? userName, string? password)
        {
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                byte[]? userCredentialsBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                return Convert.ToBase64String(userCredentialsBytes);
            }
            return string.Empty;
        }
    }
}
