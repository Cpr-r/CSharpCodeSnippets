/// This class allows you to create and compare hashed strings using the .NET implentation of PBKDF2--Rfc2898
/// (See: https://docs.microsoft.com/en- us/dotnet/api/system.security.cryptography.rfc2898derivebytes.)
/// This particular implementation also uses SHA512, so you are able to create a larger entropy than the standard 20 bytes.

using System;
using System.Security.Cryptography;

namespace MyProgram.Security
{
    public static class Hashing
    {
        /// <summary>
        /// Hashes the passed string using the passed salt byte length, key length, and number of iterations: returns result, outs hash as bytes and Base64 strng.
        /// </summary>
        /// <remarks> This method is used, for example, when you want to create a new user account and store their information safely. </remarks>
        public static bool GenerateHash(string str, int saltLength, int keyLength, int iterations, out byte[] hashBytes, out string hash)
        {
            hashBytes = null;
            hash = null;

            if (string.IsNullOrWhiteSpace(str))
                return false;

            byte[] salt = GenerateSalt(saltLength);
            byte[] key = new byte[keyLength];
            byte[] iterationsBytes = BitConverter.GetBytes(iterations);

            using (var rfc2898 = new Rfc2898DeriveBytes(str, salt, iterations, HashAlgorithmName.SHA512))
            {
                key = rfc2898.GetBytes(keyLength);
            }

            // Creating the hash.
            hashBytes = new byte[salt.Length + key.Length + iterationsBytes.Length];
            Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
            Buffer.BlockCopy(key, 0, hashBytes, key.Length, key.Length);
            Buffer.BlockCopy(iterationsBytes, 0, hashBytes, salt.Length + key.Length, iterationsBytes.Length);
            // Provide Base64 string format, i.e., the thing to store (in a database, for example).
            hash = Convert.ToBase64String(hashBytes);
            
            return true;
        }

        /// <summary>
        /// Verifies the challenge by comparing it against the hash (as bytes)--in constant time.
        /// </summary>
        /// <remarks> This method is used, for example, when you want to verify a user's account information. </remarks>
        public static bool VerifyChallenge(string hash, string challenge, int keyLength, int saltLength, int iterations)
        {
            if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(challenge))
                return false;

            byte[] hashBytes = Convert.FromBase64String(hash);
            byte[] salt = new byte[saltLength];
            byte[] iterationBytes = new byte[sizeof(int)]; // Because iterations are stored numerically as an int, simply get the size of an int.

            // Recreate the hash (as from Base64).
            Buffer.BlockCopy(hashBytes, 0, salt, 0, saltLength);
            Buffer.BlockCopy(hashBytes, keyLength + saltLength, iterationBytes, 0, iterationBytes.Length);

            // Generate a hash of the challenge so as to compare it against the stored hash; return result of challenge hash and stored hash equivalency.
            return GenerateHash(challenge, saltLength, keyLength, iterations, out byte[] challengeBytes, out _) && ConstantTimeComparison(challengeBytes, hashBytes);
        }

        /// <summary>
        /// Generates salt by virtue of the passed salt length.
        /// </summary>
        private static byte[] GenerateSalt(int saltLength)
        {
            byte[] salt = new byte[saltLength];
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                rngCryptoServiceProvider.GetBytes(salt);
            }

            return salt;
        }

        /// <summary>
        /// Compares the challenge and hash (as bytes) in constant time--so as to not leak information regarding the hash.
        /// </summary>
        private static bool ConstantTimeComparison(byte[] challengeBytes, byte[] hashBytes)
        {
            uint difference = (uint)challengeBytes.Length ^ (uint)hashBytes.Length;

            for (int i = 0; i < challengeBytes.Length && i < hashBytes.Length; i++)
            {
                difference |= (uint)(challengeBytes[i] ^ hashBytes[i]);
            }

            return difference == 0;
        }
    }
}
