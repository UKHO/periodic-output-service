using Newtonsoft.Json;

namespace UKHO.FmEssFssMock.API.Helper
{
    public class FileHelper
    {
        public static T ReadJsonFile<T>(string filePathWithFileName)
        {
            T? response = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), filePathWithFileName)));
            return response;
        }
    }
}
