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
            byte[] isoFileBytes = System.Text.Encoding.UTF8.GetBytes(targetPath);
            string hash = BitConverter.ToString(SHA1.Create().ComputeHash(isoFileBytes)).Replace("-", "");
            File.WriteAllText(targetPath + ".sha1", hash);
        }

        public void CreateXmlFile(byte[] fileContent, string targetPath)
        {
            XmlDocument doc = new();
            string xml = Encoding.UTF8.GetString(fileContent);
            doc.LoadXml(xml);

            //Create an XML declaration.
            XmlDeclaration xmlDecl = doc.CreateXmlDeclaration("1.0", "utf-8", null);

            //Add the new node to the document.
            XmlElement root = doc.DocumentElement!;
            doc.InsertBefore(xmlDecl, root);

            doc.Save(targetPath);
        }
    }
}
