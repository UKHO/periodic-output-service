using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class Catalog031ReaderFactoryWrapper : ICatalog031ReaderFactory
    {
        public ICatalog031Reader Create(byte[] catalog031File)
        {
            return new Catalog031Reader(catalog031File);
        }
    }
}
