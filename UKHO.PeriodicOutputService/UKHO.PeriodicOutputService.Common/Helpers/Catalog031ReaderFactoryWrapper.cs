using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class Catalog031ReaderFactoryWrapper : ICatalog031ReaderFactory
    {
        public ICatalog031Reader Create(byte[] catalogFileBytes)
        {
            return new Catalog031Reader(catalogFileBytes);
        }
    }
}
