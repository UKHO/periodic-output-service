
namespace UKHO.PeriodicOutputService.Common.PermitDecryption
{
    public interface IBlowfishAlgorithm
    {
        /// <summary>
        /// implements Blowfish encryption algorithm
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        bool Encrypt(byte[] buf);

        /// <summary>
        /// Implements Blowfish decryption algorithm
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        bool Decrypt(byte[] buf);
    }
}
