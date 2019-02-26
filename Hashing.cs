/// This class allows you to create hashed strings using the .NET implentation of PBKDF2--Rfc2898
/// (see: https://docs.microsoft.com/en- us/dotnet/api/system.security.cryptography.rfc2898derivebytes). 
/// This particular class also utilises SHA512, so you are able to create a larger entropy than the standard 20 bytes.
/// USAGE:
///     Usage is simple: When you have a new password to store (e.g., a user creating an account), simply invoke (externally)
///     GenerateHash(string passwordToHash) and store the returned result to your permanency, e.g., a database. 
///     When you need to verify a hashed password (e.g., a user logging in), simply retrieve the stored password hash from
///     your permanency and invoke VerifyPassword(passwordToVerify, actualPassword)--where the passwordToVerify is the user's
///     inputted password value and actualPassword is the retrieved hashed password from your permanency. The returned bool
///     from VerifyPassword will be the final result of the user's login attempt.

using System.Security.Cryptography;
using System;

namespace MyProgram.Security
{
    internal class PasswordHashing
    {
        private const int SALT_LENGTH = 32;
        private const int KEY_LENGTH = 32;
        private const int ITERATIONS = 20_000;

        /// <summary>
        /// Generates a string hash using the passed password and the default salt byte length, key length, and number of iterations.
        /// </summary>
        internal string GenerateHash(string passwordToHash)
        {
            if (string.IsNullOrWhiteSpace(passwordToHash))
            {
                return string.Empty;
            }

            byte[] salt = this.GenerateSalt(); 
            byte[] key = new byte[KEY_LENGTH];
            byte[] iterations = BitConverter.GetBytes(ITERATIONS);

            using (var rfc2898 = new Rfc2898DeriveBytes(passwordToHash, salt, ITERATIONS, HashAlgorithmName.SHA512))
            {
                key = rfc2898.GetBytes(KEY_LENGTH);
            }

            // Compile the hash (32 bytes of salt, 32 bytes of key, 4 bytes of iteration).
            byte[] hash = new byte[salt.Length + key.Length + iterations.Length];
            Buffer.BlockCopy(salt, 0, hash, 0, salt.Length);
            Buffer.BlockCopy(key, 0, hash, key.Length, key.Length);
            Buffer.BlockCopy(iterations, 0, hash, salt.Length + key.Length, iterations.Length);

            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verifies the passed password to verify by comparing it against the stored result (as bytes)--in constant time.
        /// </summary>
        internal bool VerifyPassword(string passwordToVerify, string actualPassword)
        {
            if (string.IsNullOrWhiteSpace(passwordToVerify) || string.IsNullOrWhiteSpace(actualPassword))
            {
                return false;
            }

            byte[] actualPasswordBytes = Convert.FromBase64String(actualPassword);
            byte[] salt = new byte[SALT_LENGTH];

            // I.e., 68 - 32 - 32 = 4.
            int iterationBytesLength = actualPasswordBytes.Length - KEY_LENGTH - SALT_LENGTH;
            byte[] iterationBytes = new byte[iterationBytesLength];

            Buffer.BlockCopy(actualPasswordBytes, 0, salt, 0, SALT_LENGTH);
            Buffer.BlockCopy(actualPasswordBytes, KEY_LENGTH + SALT_LENGTH, iterationBytes, 0, iterationBytes.Length);

            // Generate a hash using the stored salt and iterations--combined with the password to verify (as bytes).
            byte[] passwordToVerifyBytes = this.GenerateHash(passwordToVerify, salt, BitConverter.ToInt32(iterationBytes, 0));

            // Returns true if the bytes are equivalent--false if they are not.
            return this.ConstantTimeComparison(passwordToVerifyBytes, actualPasswordBytes);
        }

        /// <summary>
        /// Generates a salt using the default salt byte length.
        /// </summary>
        private byte[] GenerateSalt()
        {
            // Generates a 32 byte salt.
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                byte[] salt = new byte[SALT_LENGTH];
                rngCryptoServiceProvider.GetBytes(salt);

                return salt;
            }
        }

        /// <summary>
        /// Generates a byte[] hash using the passed password, salt, and iteration count.
        /// </summary>
        private byte[] GenerateHash(string passwordToHash, byte[] salt, int iterations)
        {
            if (string.IsNullOrWhiteSpace(passwordToHash) || salt == Array.Empty<byte>() || iterations <= 0)
            {
                return Array.Empty<byte>();
            }
                   
            byte[] key = new byte[KEY_LENGTH];                      
            byte[] iterationBytes = BitConverter.GetBytes(iterations);  

            using (var rfc2898 = new Rfc2898DeriveBytes(passwordToHash, salt, iterations, HashAlgorithmName.SHA512))
            {
                key = rfc2898.GetBytes(KEY_LENGTH);
            }

            // Compile the hash.
            byte[] hash = new byte[salt.Length + key.Length + iterationBytes.Length];
            Buffer.BlockCopy(salt, 0, hash, 0, salt.Length);
            Buffer.BlockCopy(key, 0, hash, key.Length, key.Length);
            Buffer.BlockCopy(iterationBytes, 0, hash, salt.Length + key.Length, iterationBytes.Length);

            return hash;
        }

        /// <summary>
        /// Compares the password to verify and the actual password (as bytes), in constant time--so as to not leak information.
        /// </summary>
        private bool ConstantTimeComparison(byte[] passwordToVerifyBytes, byte[] actualPasswordBytes)
        {
            uint difference = (uint)passwordToVerifyBytes.Length ^ (uint)actualPasswordBytes.Length;
            for (int i = 0; i < passwordToVerifyBytes.Length && i < actualPasswordBytes.Length; i++)
            {
                difference |= (uint)(passwordToVerifyBytes[i] ^ actualPasswordBytes[i]);
            }

            return difference == 0;
        }
    }
}
