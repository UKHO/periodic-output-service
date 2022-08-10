using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Net;
using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Models.Request;
using UKHO.FmEssFssMock.API.Models.Response;
using UKHO.FmEssFssMock.API.Services;

namespace UKHO.FmEssFssMock.API.Controllers
{
    [ApiController]
    public class FileShareServiceController : BaseController
    {
        private readonly FileShareService fileShareService;
        public Dictionary<string, string> ErrorsCreateBatch { get; set; }
        public Dictionary<string, string> ErrorsPutBlocksInFile { get; set; }
        public Dictionary<string, string> ErrorsCommitBatch { get; set; }
        public Dictionary<string, string> ErrorsAddFileinBatch { get; set; }
        protected IConfiguration configuration;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfiguration;

        public FileShareServiceController(IHttpContextAccessor httpContextAccessor, FileShareService fileShareService, IConfiguration configuration, IOptions<FileShareServiceConfiguration> fileShareServiceConfiguration) : base(httpContextAccessor)
        {
            this.fileShareService = fileShareService;
            ErrorsCreateBatch = new Dictionary<string, string>
            {
                { "source", "RequestBody" },
                { "description", "Either body is null or malformed." }
            };
            ErrorsPutBlocksInFile = new Dictionary<string, string>
            {
                { "source", "BatchId" },
                { "description", "Invalid or non-existing batch ID." }
            };
            ErrorsCommitBatch = new Dictionary<string, string>
            {
                { "source", "BatchId" },
                { "description", "BatchId does not exist." }
            };
            ErrorsAddFileinBatch = new Dictionary<string, string>
            {
                { "source","FileError" },
                { "description","Error while creating file" }
            };
            this.configuration = configuration;
            this.fileShareServiceConfiguration = fileShareServiceConfiguration;
        }

        [HttpPost]
        [Route("/fss/batch")]
        public IActionResult CreateBatch([FromBody] CreateBatchRequest batchRequest)
        {
            if (batchRequest != null && !string.IsNullOrEmpty(batchRequest.BusinessUnit))
            {
                var response = fileShareService.CreateBatch(batchRequest.Attributes, configuration["HOME"]);
                if (response != null)
                {
                    return Created(string.Empty, response);
                }
            }
            return BadRequest();
        }

        [HttpGet]
        [Route("/fss/batch/{batchId}")]
        public IActionResult GetBatchDetails([FromRoute] string batchId)
        {
            if (!string.IsNullOrEmpty(batchId))
            {
                BatchDetail response = fileShareService.GetBatchDetails(batchId);
                if (response != null)
                {
                    return Ok(response);
                }
            }
            return BadRequest();
        }

        [HttpGet]
        [Route("/fss/batch/{batchId}/files/{fileName}")]
        public ActionResult DownloadFile([FromRoute] string batchId, [FromRoute] string fileName)
        {
            byte[] bytes = null;

            if (!string.IsNullOrEmpty(fileName))
            {
                bytes = fileShareService.GetFileData(configuration["HOME"], batchId, fileName);
            }

            return File(bytes, "application/octet-stream", fileName);
        }

        [HttpPut]
        [Route("/fss/batch/{batchId}/files/{fileName}/{blockId}")]
        [Produces("application/json")]
        public IActionResult UploadBlockOfFile([FromRoute, SwaggerSchema(Format = "GUID"), SwaggerParameter(Required = true)] string batchId,
                                                           [FromRoute, SwaggerParameter(Required = true)] string fileName,
                                                           [FromRoute, SwaggerParameter(Required = true)] string blockId,
                                                           [FromHeader(Name = "Content-Length"), SwaggerSchema(Format = ""), SwaggerParameter(Required = true)] long? contentLength,
                                                           [FromHeader(Name = "Content-MD5"), SwaggerSchema(Format = "byte"), SwaggerParameter(Required = true)] string contentMD5,
                                                           [FromHeader(Name = "Content-Type"), SwaggerSchema(Format = "MIME"), SwaggerParameter(Required = true)] string contentType,
                                                           [FromBody] object data)
        {
            if (!string.IsNullOrEmpty(batchId) && data != null && !string.IsNullOrEmpty(blockId))
            {
                var response = fileShareService.UploadBlockOfFile(batchId, configuration["HOME"], fileName);
                if (response)
                {
                    return StatusCode((int)HttpStatusCode.Created);
                }
            }
            return BadRequest();
        }

        [HttpPut]
        [Route("/fss/batch/{batchId}/files/{fileName}")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public IActionResult PutBlocksInFile([FromRoute, SwaggerSchema(Format = "GUID"), SwaggerParameter(Required = true)] string batchId,
                                             [FromRoute, SwaggerParameter(Required = true)] string fileName,
                                             [FromBody, SwaggerParameter(Required = true)] FileCommitPayload payload)
        {
            if (!string.IsNullOrEmpty(batchId) && !string.IsNullOrEmpty(fileName) && payload != null)
            {
                var response = fileShareService.CheckBatchWithFileExist(batchId, fileName, configuration["HOME"]);
                if (response)
                {
                    return StatusCode((int)HttpStatusCode.NoContent);
                }
            }
            return BadRequest(new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsPutBlocksInFile });
        }

        [HttpPut]
        [Route("/fss/batch/{batchId}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult CommitBatch([FromRoute] string batchId, [FromBody] List<BatchCommitRequest> body)
        {
            if (!string.IsNullOrEmpty(batchId) && body != null)
            {
                var response = fileShareService.CheckBatchWithFileExist(batchId, body.Select(a => a.FileName).FirstOrDefault(), configuration["HOME"]);
                if (response)
                {
                    return Accepted(new BatchCommitResponse() { Status = new Status { URI = $"/batch/{batchId}/status" } });
                }
            }
            return BadRequest(new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsCommitBatch });
        }


        [HttpPost]
        [Route("/fss/batch/{batchId}/files/{fileName}")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public IActionResult AddFileToBatch([FromRoute, SwaggerSchema(Format = "GUID"), SwaggerParameter(Required = true)] string batchId,
                                            [FromRoute, SwaggerParameter(Required = true)] string fileName,
                                            [FromHeader(Name = "X-MIME-Type"), SwaggerSchema(Format = "MIME")] string contentType,
                                            [FromHeader(Name = "X-Content-Size"), SwaggerSchema(Format = ""), SwaggerParameter(Required = true)] long? xContentSize,
                                            [FromBody] FileRequest attributes)
        {
            if (!string.IsNullOrEmpty(batchId))
            {
                var response = fileShareService.AddFile(batchId, fileName, configuration["HOME"]);
                if (response)
                {
                    return StatusCode(StatusCodes.Status201Created);
                }
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { CorrelationId = GetCurrentCorrelationId(), Errors = ErrorsAddFileinBatch });
        }

        [HttpGet]
        [Route("/fss/batch/{batchId}/status")]
        [Produces("application/json")]
        public IActionResult GetBatchStatus([FromRoute, Required] string batchId)
        {
            if (!string.IsNullOrEmpty(batchId))
            {
                BatchStatusResponse batchStatusResponse = fileShareService.GetBatchStatus(batchId, configuration["HOME"]);
                if (batchStatusResponse.Status == "Committed")
                {
                    return new OkObjectResult(batchStatusResponse);
                }
            }
            return Unauthorized();
        }

        [HttpPost]
        [Route("/fss/cleanUp")]
        public IActionResult CleanUp([FromBody] List<string> batchId)
        {
            if (batchId != null && batchId.Count > 0)
            {
                var response = fileShareService.CleanUp(batchId, configuration["HOME"]);
                if (response)
                {
                    return Ok();
                }
            }
            return BadRequest();
        }
    }
}
