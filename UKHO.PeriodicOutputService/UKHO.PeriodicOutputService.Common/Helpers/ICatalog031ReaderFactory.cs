using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface ICatalog031ReaderFactory
    {
        ICatalog031Reader Create(byte[] catalogFileBytes);
    }
}
