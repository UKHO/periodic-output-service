using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace UKHO.FmEssFssMock.API.Filters
{
    public class BinaryRequestBodyFormatter : InputFormatter
    {
        public BinaryRequestBodyFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/octet-stream"));
        }

        public override bool CanRead(InputFormatterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return context.HttpContext.Request.ContentType == "application/octet-stream";
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            var contentType = context.HttpContext.Request.ContentType;

            if (contentType == "application/octet-stream")
            {
                using var memoryStream = new MemoryStream();
                await request.Body.CopyToAsync(memoryStream);
                var content = memoryStream.ToArray();
                return await InputFormatterResult.SuccessAsync(content);
            }
            return await InputFormatterResult.FailureAsync();
        }
    }
}

