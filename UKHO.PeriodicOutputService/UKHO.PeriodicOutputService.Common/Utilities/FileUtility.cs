using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using DiscUtils.Iso9660;
namespace UKHO.PeriodicOutputService.Common.Utility
{
    [ExcludeFromCodeCoverage]
    public class FileUtility : IFileUtility
    {
        public void CreateISOImage(IEnumerable<string> srcFiles, string targetPath, string directoryPath)
        {
            var iso = new CDBuilder
            {
                UseJoliet = true,
                VolumeIdentifier = "FullAVCSExchangeSet"
            };

            foreach (string? file in srcFiles)
            {
                var fi = new FileInfo(file);
                if (fi.Directory.Name == directoryPath)
                {
                    iso.AddFile($"{fi.Name}", fi.FullName);
                    continue;
                }
                string? srcDir = fi.Directory.FullName.Replace(directoryPath, "").TrimEnd('\\');
                iso.AddDirectory(srcDir);
                iso.AddFile($"{srcDir}\\{fi.Name}", fi.FullName);
            }
            iso.Build(targetPath);
        }

        public void CreateSha1File(string targetPath)
        {
            byte[] isoFileBytes = System.Text.Encoding.UTF8.GetBytes(targetPath);
            string hash = BitConverter.ToString(SHA1.Create().ComputeHash(isoFileBytes)).Replace("-", "");
            File.WriteAllText(targetPath + ".sha1", hash);
        }
    }
}
