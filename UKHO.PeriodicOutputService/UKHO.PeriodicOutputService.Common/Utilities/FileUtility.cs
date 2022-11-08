using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using DiscUtils.Iso9660;

namespace UKHO.PeriodicOutputService.Common.Utilities
{
    [ExcludeFromCodeCoverage]
    public class FileUtility : IFileUtility
    {
        public void CreateISOImage(IEnumerable<string> srcFiles, string targetPath, string directoryPath)
        {
            var iso = new CDBuilder
            {
                UseJoliet = true,
                VolumeIdentifier = new DirectoryInfo(directoryPath).Name
            };

            foreach (string? file in srcFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo!.Directory!.Name == directoryPath)
                {
                    iso.AddFile($"{fileInfo.Name}", fileInfo.FullName);
                    continue;
                }
                string srcDir = fileInfo.Directory.FullName.Replace(directoryPath, "").TrimEnd('\\');
                iso.AddDirectory(srcDir);
                iso.AddFile($"{srcDir}\\{fileInfo.Name}", fileInfo.FullName);
            }
            iso.Build(targetPath);
        }

        public void CreateSha1File(string targetPath)
        {
            FileInfo fileInfo = new(targetPath);
            using Stream fileStream = fileInfo.OpenRead();

            string hash = BitConverter.ToString(SHA1.Create().ComputeHash(fileStream)).Replace("-", "");
            File.WriteAllText(targetPath + ".sha1", hash);
        }

        public void CreateXmlFile(byte[] fileContent, string targetPath)
        {
            XmlDocument doc = new();
            var xml = Encoding.UTF8.GetString(fileContent);

            ////Added below code to remove the ? hidden text from the XML Response text
            var byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

            if (xml.StartsWith(byteOrderMarkUtf8, StringComparison.Ordinal))
            {
                xml = xml.Remove(0, byteOrderMarkUtf8.Length);
            }

            doc.LoadXml(xml);

            doc.Save(targetPath);
        }
    }
}
