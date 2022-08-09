using System.Text.Json;

namespace UKHO.FmEssFssMock.API.Helper
{
    public class FileHelper
    {
        public static T ReadJsonFile<T>(string filePathWithFileName)
        {
            JsonSerializerOptions Options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            var folderDetails = Path.Combine(Directory.GetCurrentDirectory(), filePathWithFileName);
            var myJsonString = File.ReadAllText(folderDetails);
            return JsonSerializer.Deserialize<T>(myJsonString, Options);
        }
    }
}
