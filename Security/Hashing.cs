/// This class allows you to create hashed strings using the .NET implentation of PBKDF2--Rfc2898
/// (see: https://docs.microsoft.com/en- us/dotnet/api/system.security.cryptography.rfc2898derivebytes). 
/// This particular implementation also uses SHA512, so you are able to create a larger entropy than the standard 20 bytes.

using System;
using System.Security.Cryptography;

namespace MyProgram.Security
{
    internal static class Hashing
    {
        /// <summary>
        /// Hashes the passed string using the passed salt byte length, key length, and number of iterations--outputting the hash as bytes and hash as string.
        /// </summary>
        internal static bool GenerateHash(in string stringToHash, in int saltLength, in int keyLength, in int iterations, out byte[] hashBytes, out string hash)
        {
            hash = string.Empty;
            hashBytes = Array.Empty<byte>();

            if (string.IsNullOrWhiteSpace(stringToHash))
                return false;

            byte[] salt = GenerateSalt(in saltLength);
            byte[] key = new byte[keyLength];
            byte[] iterationsBytes = BitConverter.GetBytes(iterations);

            using (var rfc2898 = new Rfc2898DeriveBytes(stringToHash, salt, iterations, HashAlgorithmName.SHA512))
            {
                key = rfc2898.GetBytes(keyLength);
            }

            // Prepare the hash.
            hashBytes = new byte[salt.Length + key.Length + iterationsBytes.Length];

            // Compile the hash.
            Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
            Buffer.BlockCopy(key, 0, hashBytes, key.Length, key.Length);
            Buffer.BlockCopy(iterationsBytes, 0, hashBytes, salt.Length + key.Length, iterationsBytes.Length);

            // Provide Base64 string format.
            hash = Convert.ToBase64String(hashBytes);

            return true;
        }

        /// <summary>
        /// Verifies the challenge by comparing it against the hash (as bytes)--in constant time.
        /// </summary>
        internal static bool VerifyChallenge(in string challenge, in string hash, in int saltLength, in int keyLength, in int iterations)
        {
            if (string.IsNullOrWhiteSpace(challenge) || string.IsNullOrWhiteSpace(hash))
                return false;

            byte[] hashBytes = Convert.FromBase64String(hash);
            byte[] salt = new byte[saltLength];

            // Because iterations are stored numerically as an int, simply get the size of an int (typically 4 bytes).
            byte[] iterationBytes = new byte[sizeof(int)];

            Buffer.BlockCopy(hashBytes, 0, salt, 0, saltLength);
            Buffer.BlockCopy(hashBytes, keyLength + saltLength, iterationBytes, 0, iterationBytes.Length);

            // Generate a hash of the challenge so as to compare against the stored hash.
            if (GenerateHash(in challenge, in saltLength, in keyLength, in iterations, out byte[] challengeBytes, out _))
                return ConstantTimeComparison(in challengeBytes, in hashBytes);
            else
                return false;
        }

        /// <summary>
        /// Generates a salt using the passed salt byte length.
        /// </summary>
        private static byte[] GenerateSalt(in int saltLength)
        {
            byte[] salt = new byte[saltLength];

            // Generate the salt.
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                rngCryptoServiceProvider.GetBytes(salt);
            }

            return salt;
        }

        /// <summary>
        /// Compares the challenge and hash (as bytes) in constant time--so as to not leak information.
        /// </summary>
        private static bool ConstantTimeComparison(in byte[] challengeBytes, in byte[] hashBytes)
        {
            uint difference = (uint)challengeBytes.Length ^ (uint)hashBytes.Length;

            for (int i = 0; i < challengeBytes.Length && i < hashBytes.Length; i++)
                difference |= (uint)(challengeBytes[i] ^ hashBytes[i]);

            return difference == 0;
        }
    }
}
