using System;
using System.Security.Cryptography;

namespace CuahangNongduoc
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public static string CreateSalt()
        {
            byte[] salt = new byte[SaltSize];

            RNGCryptoServiceProvider random =
                new RNGCryptoServiceProvider();

            random.GetBytes(salt);

            return ToHex(salt);
        }

        public static string HashPassword(string password, string saltHex)
        {
            byte[] salt = FromHex(saltHex);
            Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations);
            try
            {
                return ToHex(deriveBytes.GetBytes(HashSize));
            }
            finally
            {
                deriveBytes.Reset();
            }
        }

        public static bool VerifyPassword(string password, string saltHex, string expectedHash)
        {
            if (password == null || saltHex == null || expectedHash == null)
                return false;

            string actualHash = HashPassword(password, saltHex);
            if (actualHash.Length != expectedHash.Length)
                return false;

            int difference = 0;
            int i;
            for (i = 0; i < actualHash.Length; i++)
                difference |= actualHash[i] ^ expectedHash[i];
            return difference == 0;
        }

        private static string ToHex(byte[] bytes)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(bytes.Length * 2);
            int i;
            for (i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2"));
            return builder.ToString();
        }

        private static byte[] FromHex(string value)
        {
            if (value == null || value.Length % 2 != 0)
                throw new ArgumentException("Salt không hợp lệ.");

            byte[] bytes = new byte[value.Length / 2];
            int i;
            for (i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            return bytes;
        }
    }
}
