namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public static class FileContentHelper
    {
        public static void DeleteTempDirectory(string tempFolder)
        {
            string path = Path.GetTempPath() + tempFolder;

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
