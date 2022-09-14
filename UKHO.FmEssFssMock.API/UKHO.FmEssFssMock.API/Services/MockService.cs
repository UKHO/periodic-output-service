using UKHO.FmEssFssMock.API.Helpers;
using UKHO.FmEssFssMock.Enums;

namespace UKHO.FmEssFssMock.API.Services
{
    public class MockService
    {
        public bool CleanUp(string homeDirectoryPath)
        {
            return FileHelper.CleanUp(homeDirectoryPath);
        }
    }
}
