using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CreateDirectory(string folderPath);
        byte[] ConvertStreamToByteArray(Stream input);
        void CreateFileCopy(string filePath, Stream stream);
    }
}
