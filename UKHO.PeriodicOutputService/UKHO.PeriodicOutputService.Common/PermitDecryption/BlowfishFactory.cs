using System.Diagnostics.CodeAnalysis;
using System.Runtime.Caching;

namespace UKHO.PeriodicOutputService.Common.PermitDecryption
{

    [ExcludeFromCodeCoverage]
    public class BlowfishFactory : IBlowfishFactory
    {
        private static readonly ObjectCache Cache = MemoryCache.Default;
        /// <summary>
        /// Gets the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cachingOptions"></param>
        /// <returns></returns>
        public IBlowfishAlgorithm Get(byte[] key, CachingOptions cachingOptions = CachingOptions.Cache)
        {
            var lazy = new Lazy<BlowfishAlgorithm>(() => new BlowfishAlgorithm(key));

            if (cachingOptions == CachingOptions.NoCache)
                return lazy.Value;

            var policy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(10) };

            var cacheKey = "BFAlg" + ToHex(key);
            var cacheObject = Cache.AddOrGetExisting(cacheKey, lazy, policy) ?? lazy;
            return ((Lazy<BlowfishAlgorithm>)cacheObject).Value;
        }

        private static string ToHex(byte[] data)
        {
            if (data == null)
                return null;
            char[] c = new char[data.Length * 2];
            int b;
            for (int i = 0; i < data.Length; i++)
            {
                b = data[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = data[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }
            return new string(c);
        }
    }

    public enum CachingOptions
    {
        NoCache,
        Cache
    }
}
