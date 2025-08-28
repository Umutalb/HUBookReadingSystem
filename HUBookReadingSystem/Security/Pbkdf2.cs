using System.Security.Cryptography;

namespace HUBookReadingSystem.Security
{
    public static class Pbkdf2
    {
        public const int Iterations = 120_000;
        public const int HashSize = 32; // 256-bit

        public static bool Verify(string pin, byte[] salt, byte[] expectedHash)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, Iterations, HashAlgorithmName.SHA256);
            var actual = pbkdf2.GetBytes(HashSize);
            return FixedTimeEquals(actual, expectedHash);
        }

        private static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}

