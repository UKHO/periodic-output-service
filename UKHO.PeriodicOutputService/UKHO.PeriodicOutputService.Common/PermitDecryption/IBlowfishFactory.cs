namespace UKHO.PeriodicOutputService.Common.PermitDecryption
{
    public interface IBlowfishFactory
    {
        /// <summary>
        /// Gets the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cachingOptions"></param>
        /// <returns></returns>
        IBlowfishAlgorithm Get(byte[] key, CachingOptions cachingOptions = CachingOptions.Cache);
    }
}
