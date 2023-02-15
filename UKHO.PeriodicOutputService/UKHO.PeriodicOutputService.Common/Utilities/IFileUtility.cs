namespace UKHO.PeriodicOutputService.Common.Utilities
{
    public interface IFileUtility
    {
        void CreateISOImage(IEnumerable<string> srcFiles, string targetPath, string directoryPath, string volumeIdentifier);

        void CreateSha1File(string targetPath);
        void CreateXmlFile(byte[] fileContent, string targetPath);
    }
}
