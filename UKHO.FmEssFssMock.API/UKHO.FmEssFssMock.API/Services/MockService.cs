using UKHO.FmEssFssMock.API.Helpers;
using UKHO.FmEssFssMock.Enums;

namespace UKHO.FmEssFssMock.API.Services
{
    public class MockService
    {
        private readonly string currentTestFileName = "CurrentTestCase.txt";

        public void UpdatePOSTestCase(PosTestCase posTestCase, string homeDirectoryPath)
        {
            string destPath = Path.Combine(homeDirectoryPath, currentTestFileName);

            if (!File.Exists(destPath))
            {
                File.Create(destPath).Close();
            }
            File.WriteAllText(destPath, "");
            File.WriteAllText(destPath, posTestCase.ToString());
        }

        public PosTestCase GetCurrentPOSTestCase(string homeDirectoryPath)
        {
            string destPath = Path.Combine(homeDirectoryPath, currentTestFileName);

            string readText = File.ReadAllText(destPath);
            Enum.TryParse(readText, true, out PosTestCase posTestCase);
            return posTestCase;
        }

        public bool MoveFmFolder(string homeDirectoryPath)
        {
            string sourcePath = Path.Combine(Environment.CurrentDirectory, @"Data", "FM");
            string targetPath = Path.Combine(homeDirectoryPath, "FM");

            Directory.CreateDirectory(targetPath);

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
            return true;
        }

        public bool CleanUp(string homeDirectoryPath)
        {
            return FileHelper.CleanUp(homeDirectoryPath);
        }
    }
}
