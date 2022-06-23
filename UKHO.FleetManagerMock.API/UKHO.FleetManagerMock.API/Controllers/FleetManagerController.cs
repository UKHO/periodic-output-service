using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using UKHO.FleetManagerMock.API.Models;

namespace UKHO.FleetManagerMock.API.Controllers
{
    [ApiController]
    public class FleetManagerController : ControllerBase
    {
        protected IConfiguration configuration;
        
        public FleetManagerController(IConfiguration configuration)
        {
            this.configuration = configuration;
            
        }

        [HttpGet]
        [Route("/auth/unp")]
        public IActionResult GetJwtAuthUnpToken([FromHeader(Name = "userPass")] string userPass, [FromHeader(Name = "Ocp-Apim-Subscription-Key")] string userPass1)
        {
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>();
            foreach (var header in Request.Headers)
            {
                requestHeaders.Add(header.Key, header.Value);
            }

            string userName = this.configuration.GetSection("FleetManagerB2BApiConfiguration")["userName"];
            string password = this.configuration.GetSection("FleetManagerB2BApiConfiguration")["password"];
            string subscriptionkey = this.configuration.GetSection("FleetManagerB2BApiConfiguration")["subscriptionKey"];

            string responseContent = string.Empty;
            FleetManagerGetAuthTokenResponse fleetMangerGetAuthTokenResponse = new FleetManagerGetAuthTokenResponse();
            string base64Credentials = GetBase64EncodedCredentials(userName, password);
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string AuthToken = string.Empty;
            if (userPass == base64Credentials)
            {

                var token = "{token':'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJiNjZkMGM1Zi01N2IyLTQ4ZTUtOWNlZS02YjE4YjE2YzJjNWYiLCJpYXQiOiIxNjU1OTA1NTYzIiwic3ViIjoiQjJCTWFzdGVrIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6IkIyQk1hc3RlayIsInVzZXJpZCI6IjUxNjAyIiwic2VjcmV0IjoiMTAwMDpVZ05XSjc5MkpTNVNJMXlkYkxrMmVQSmlQQzVJN1pVWDpqRG5QdnMvNGtxclRRalBGd085aG9IL3B4anRFKzJSeiIsImVuZHBvaW50L0dldCBDYXRhbG9ndWVzIjoidHJ1ZSIsImVuZHBvaW50L0dldCBUb2tlbiB3aXRoIExvZ2luIjoidHJ1ZSIsImVuZHBvaW50L1JlbmV3IFRva2VuIjoidHJ1ZSIsImV4cCI6MTY1NTkwOTE2MywiaXNzIjoiVUtIT19GTSIsImF1ZCI6IlVLSE9fRk0ifQ.-pDCe2NLzppbK79BA3S9_6AlUwOdAAeYDUkrRJdTtgs}";
                //await GetJwtAuthUnpToken(HttpMethod.Get, base64Credentials , subscriptionKey) ;
                httpResponse.StatusCode = System.Net.HttpStatusCode.OK;
                // fleetMangerGetAuthTokenResponse.StatusCode = httpResponse.StatusCode;

                if (httpResponse.IsSuccessStatusCode)
                {
                    responseContent = token.Trim();
                    AuthToken = responseContent;
                }
            }
            return Ok(AuthToken);
        }

        [HttpGet]
        [Route("/catalogues/{catalogueId}")]
        public IActionResult GetCatalogue([FromHeader(Name = "token")] string token, [FromHeader(Name = "Ocp-Apim-Subscription-Key")] string token1)
        {
            string path = this.configuration.GetSection("FileDirectoryPath")["response"];
            XDocument doc = XDocument.Load(path);
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            Encoding encoding = Encoding.UTF8;
            byte[] docAsBytes = encoding.GetBytes(doc.ToString());
            if (docAsBytes == null)
            {
                httpResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                return BadRequest(httpResponse.StatusCode);
            }
            return File(docAsBytes, "application/xml", path);
        }

        [NonAction]
        public static string GetBase64EncodedCredentials(string userName, string password)
        {
            var userCredentialsBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
            return Convert.ToBase64String(userCredentialsBytes);
        }

    }
}
