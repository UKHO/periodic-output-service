using UKHO.PeriodicOutputService.Common.Models.Fss.Request;
using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class Catalog031FilterHelper: ICatalog031FilterHelper
    {
        private readonly IFileSystemHelper _fileSystemHelper;

        public Catalog031FilterHelper(IFileSystemHelper fileSystemHelper)
        {
            _fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
        }

        public void RemoveReadmeEntryAndUpdateCatalog(string catalogFilePath)
        {
            byte[] catalogFileBytes = _fileSystemHelper.GetFileInBytes(new UploadFileBlockRequestModel
            {
                FullFileName = catalogFilePath,
                Length = (int)new FileInfo(catalogFilePath).Length,
                Offset = 0
            });

            var details = new Catalog031Reader(catalogFileBytes);

            var list = details.ReadCatalogue();

            var catBuilder = new Catalog031BuilderFactory().Create();

            foreach (var lst in list)
            {
                if (!lst.FileLocation.Equals("README.TXT") && !lst.FileLocation.Equals("CATALOG.031"))
                {
                    catBuilder.Add(lst);
                }
            }

            var cat031Bytes = catBuilder.WriteCatalog("V01X01");

            _fileSystemHelper.DeleteFile(catalogFilePath);

            using (var memoryStream = new MemoryStream(cat031Bytes))
            {
                _fileSystemHelper.CreateFileCopy(catalogFilePath, memoryStream);
            }
        }
    }
}
