using System.IO.Abstractions;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IFileInfoHelper
    {
        IFileInfo GetFileInfo(string filePath);
    }
}
