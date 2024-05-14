using UKHO.PeriodicOutputService.Common.Enums;

namespace UKHO.PeriodicOutputService.Common.PermitDecryption
{
    public interface IS63Crypt
    {
        void Dword2Byte(uint i, byte[] arr, int offset);
        uint Byte2Dword(byte[] arr, int offset);
        (CryptResult, byte[], byte[]) GetEncKeysFromPermit(string permit, byte[] hwIdBytes);
    }
}
