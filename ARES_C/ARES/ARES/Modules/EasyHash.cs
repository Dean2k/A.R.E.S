using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ARES.Modules
{
    internal static class EasyHash
    {
        internal static byte[] GetMD5(byte[] input)
        {
            using (var sha256 = MD5.Create())
                return sha256.ComputeHash(input);
        }

        internal static byte[] GetSHA1(byte[] input)
        {
            using (var sha256 = SHA1.Create())
                return sha256.ComputeHash(input);
        }

        internal static byte[] GetSHA256(byte[] input)
        {
            using (var sha256 = SHA256.Create())
                return sha256.ComputeHash(input);
        }

        internal static byte[] GetSHA512(byte[] input)
        {
            using (var sha256 = SHA512.Create())
                return sha256.ComputeHash(input);
        }

        internal static string GetMD5String(byte[] input) =>
            BitConverter.ToString(GetMD5(input)).Replace("-", string.Empty).ToLower();

        internal static string GetSHA1String(byte[] input) =>
            BitConverter.ToString(GetSHA1(input)).Replace("-", string.Empty).ToLower();

        internal static string GetSHA256String(byte[] input) =>
            BitConverter.ToString(GetSHA256(input)).Replace("-", string.Empty).ToLower();

        internal static string GetSHA512String(byte[] input) =>
            BitConverter.ToString(GetSHA512(input)).Replace("-", string.Empty).ToLower();
    }
}
