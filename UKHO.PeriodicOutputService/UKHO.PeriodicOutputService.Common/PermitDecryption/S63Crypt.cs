using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace UKHO.PeriodicOutputService.Common.PermitDecryption
{
    [ExcludeFromCodeCoverage]
    public static class S63Crypt
    {
        public enum CryptResult { HWIDFmtErr = 0, CRCErr = 1, Ok = 2 };

        public static void Dword2Byte(uint i, byte[] arr, int offset)
        {
            arr[offset + 3] = (byte)(i & 0xFF);
            arr[offset + 2] = (byte)((i >> 8) & 0xFF);
            arr[offset + 1] = (byte)((i >> 16) & 0xFF);
            arr[offset] = (byte)((i >> 24) & 0xFF);
        }

        public static uint Byte2Dword(byte[] arr, int offset)
        {
            return (((uint)arr[offset] << 24) + ((uint)arr[offset + 1] << 16) + ((uint)arr[offset + 2] << 8) + arr[offset + 3]);
        }

        private static string ToHex(this byte[] data)
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

        public static CryptResult GetEncKeysFromPermit(string permit, byte[] hwIdBytes, ref byte[] ck1, ref byte[] ck2)
        {
            if (hwIdBytes.Length < 5)
                return CryptResult.HWIDFmtErr;

            //prepare HW_ID
            var hwId = new byte[6];
            Array.Copy(hwIdBytes, hwId, 5);
            hwId[5] = hwId[0];

            //convert permit string to Byte array
            byte[] pmtar = Encoding.UTF8.GetBytes(permit);

            //--------- calculate CRC32 for left part of Cell permit(48 bytes)
            var crcProc = new ICSharpCode.SharpZipLib.Checksum.Crc32();
            ArraySegment<byte> arraySegment = new(pmtar, 0, 48);
            crcProc.Update(arraySegment);

            var bcrc32 = new byte[8];
            Dword2Byte((uint)crcProc.Value, bcrc32, 0);//convert DWORD to BYTES
            bcrc32[4] = bcrc32[5] = bcrc32[6] = bcrc32[7] = 0x04;   //padding for Blowfish

            //--- encrypt CRC32 by Blowfish alghorithm -------------------
            var bf = (new BlowfishFactory()).Get(hwId, CachingOptions.Cache);
            bf.Encrypt(bcrc32);

            //convert the result of crc encryption to hexadecimal string presentation
            var hexcrc = bcrc32.ToHex();

            //check permnit validity
            if (hexcrc != permit.Substring(48, 16))
                return CryptResult.CRCErr; // Cell Permit is invalid (checksum is incorrect)

            ck1 = new byte[8];
            ck2 = new byte[8];

            int i;
            int j;
            int k;

            for (i = 0, j = 16, k = 32; k < 48; j += 2, k += 2)
            {
                ck1[i] = byte.Parse(permit.Substring(j, 2), NumberStyles.AllowHexSpecifier);
                ck2[i++] = byte.Parse(permit.Substring(k, 2), NumberStyles.AllowHexSpecifier);
            }

            bf.Decrypt(ck1);
            bf.Decrypt(ck2);
            Array.Resize(ref ck1, 5);
            Array.Resize(ref ck2, 5);

            return CryptResult.Ok;
        }
    }
}
