using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Models.Request;
using UKHO.FmEssFssMock.API.Models.Response;
using UKHO.FmEssFssMock.API.Services;

namespace UKHO.FmEssFssMock.API.Controllers
{
    [ApiController]
    public class _fileShareServiceController : BaseController
    {
        private readonly FileShareService _fileShareService;

        private readonly IConfiguration _configuration;
        private readonly IOptions<FileShareServiceConfiguration> _fssConfiguration;

        public Dictionary<string, string> ErrorsCreateBatch { get; set; }
        public Dictionary<string, string> ErrorsPutBlocksInFile { get; set; }
        public Dictionary<string, string> ErrorsCommitBatch { get; set; }
        public Dictionary<string, string> ErrorsAddFileinBatch { get; set; }

        private readonly string _homeDirectoryPath;

        public _fileShareServiceController(IHttpContextAccessor httpContextAccessor, FileShareService fileShareService, IConfiguration configuration, IOptions<FileShareServiceConfiguration> fssConfiguration) : base(httpContextAccessor)
        {
            _fileShareService = fileShareService;
            _fssConfiguration = fssConfiguration;

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
            _configuration = configuration;
            _homeDirectoryPath = Path.Combine(_configuration["HOME"], _configuration["POSFolderName"]);

        }

        [HttpPost]
        [Route("/fss/batch")]
        public IActionResult CreateBatch([FromBody] CreateBatchRequest batchRequest)
        {
            if (batchRequest != null && !string.IsNullOrEmpty(batchRequest.BusinessUnit))
            {
                BatchResponse? response = _fileShareService.CreateBatch(batchRequest.Attributes, _homeDirectoryPath);
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
                string path = Path.Combine(_homeDirectoryPath, batchId);
                if (Directory.Exists(path))
                {
                    BatchDetail response = _fileShareService.GetBatchDetails(batchId, _homeDirectoryPath);
                    if (response != null)
                    {
                        return Ok(response);
                    }
                }
                else
                {
                    return NotFound();
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
                bytes = _fileShareService.GetFileData(_homeDirectoryPath, batchId, fileName);
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
                bool response = _fileShareService.UploadBlockOfFile(batchId, _homeDirectoryPath, fileName);
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
                bool response = _fileShareService.CheckBatchWithFileExist(batchId, fileName, _homeDirectoryPath);
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
                bool response = _fileShareService.CheckBatchWithFileExist(batchId, body.Select(a => a.FileName).FirstOrDefault(), _homeDirectoryPath);
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
                bool response = _fileShareService.AddFile(batchId, fileName, _homeDirectoryPath);
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
                BatchStatusResponse batchStatusResponse = _fileShareService.GetBatchStatus(batchId, _homeDirectoryPath);
                if (batchStatusResponse.Status == "Committed" || batchStatusResponse.Status == "CommitInProgress")
                {
                    return new OkObjectResult(batchStatusResponse);
                }
            }
            return NotFound();
        }

        [HttpGet]
        [Route("/fss/batch")]
        public IActionResult GetBatchResponse([FromQuery(Name = "$filter")] string filter = "")
        {
            if (!string.IsNullOrWhiteSpace(filter))
            {
                string responseFilePath = Path.Combine(_fssConfiguration.Value.FssDataDirectoryPath, _fssConfiguration.Value.FssInfoResponseFileName);
                SearchBatchResponse response = _fileShareService.GetBatchResponse(filter, responseFilePath, _homeDirectoryPath);

                if (response != null)
                {
                    return Ok(response);
                }
            }
            return BadRequest();
        }
    }
}
