using UKHO.FmEssFssMock.API.Helpers;
using UKHO.FmEssFssMock.Enums;

namespace UKHO.FmEssFssMock.API.Services
{
    public class MockService
    {
        private readonly string destPath = Path.Combine(Environment.CurrentDirectory, @"Data", "CurrentTestCase.txt");
        public void UpdatePOSTestCase(PosTestCase posTestCase)
        {
            File.WriteAllText(destPath, "");
            File.WriteAllText(destPath, posTestCase.ToString());
        }

        public PosTestCase GetCurrentPOSTestCase()
        {
            string readText = File.ReadAllText(destPath);
            Enum.TryParse(readText, true, out PosTestCase posTestCase);
            return posTestCase;
        }

        public bool CleanUp(string homeDirectoryPath)
        {
            return FileHelper.CleanUp(homeDirectoryPath);
        }
    }
}
