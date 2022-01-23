using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace JsonFlatFileDataStore
{
    /// <summary>
    /// AES256 class implements the OpenSSL compatible cipher AES/256/CBC/PKCS7
    /// <see href="https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rijndaelmanaged?view=net-5.0">RijndaelManaged</see>
    /// <see href="https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?redirectedfrom=MSDN&view=net-5.0">AES</see>
    /// <see href="https://github.com/mervick/aes-everywhere">mervick/aes-everywhere</see>
    /// </summary>
    class Aes256
    {
        public const int BlockSize = 16;
        public const int KeyLen = 32;
        public const int IvLen = 16;

        private byte[] key;
        private byte[] iv;

        /// <summary>
        /// Encrypt input text with the password using random salt.
        /// Returns base64 decoded encrypted string.
        /// </summary>
        /// <param name="text">Input text to encrypt</param>
        /// <param name="passphrase">Passphrase</param>
        public string Encrypt(string text, string passphrase)
        {
            return Encrypt(Encoding.UTF8.GetBytes(text), passphrase);
        }

        /// <summary>
        /// Encrypt input bytes with the password using random salt.
        /// Returns base64 decoded encrypted string.
        /// </summary>
        /// <param name="data">Input data (in bytes) to encrypt</param>
        /// <param name="passphrase">Passphrase</param>
        public string Encrypt(byte[] data, string passphrase)
        {
            using RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
            byte[] salt = new byte[8];
            random.GetBytes(salt);

            DeriveKeyAndIv(passphrase, salt);

            byte[] encrypted;
            using (RijndaelManaged aes = new RijndaelManaged())
            {
                aes.BlockSize = BlockSize * 8;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                csEncrypt.Write(data, 0, data.Length);
                csEncrypt.FlushFinalBlock();
                encrypted = msEncrypt.ToArray();
            }
            return Convert.ToBase64String(Concat(Concat("Salted__", salt), encrypted));
        }

        /// <summary>
        /// Derypt encrypted text with the password using random salt.
        /// Returns the decrypted string.
        /// </summary>
        /// <param name="encrypted">Encrypted text to decrypt</param>
        /// <param name="passphrase">Passphrase</param>
        public string Decrypt(string encrypted, string passphrase)
        {
            return Encoding.UTF8.GetString(DecryptToBytes(encrypted, passphrase));
        }

        /// <summary>
        /// Derypt encrypted data with the password using random salt.
        /// Returns the decrypted bytes.
        /// </summary>
        /// <param name="encrypted">Encrypted data to decrypt</param>
        /// <param name="passphrase">Passphrase</param>
        public byte[] DecryptToBytes(string encrypted, string passphrase)
        {
            byte[] ct = Convert.FromBase64String(encrypted);
            if (ct == null || ct.Length <= 0)
            {
                return Array.Empty<byte>();
            }

            byte[] salted = new byte[8];
            Array.Copy(ct, 0, salted, 0, 8);

            if (Encoding.UTF8.GetString(salted) != "Salted__")
            {
                return Array.Empty<byte>();
            }

            byte[] salt = new byte[8];
            Array.Copy(ct, 8, salt, 0, 8);

            byte[] cipherText = new byte[ct.Length - 16];
            Array.Copy(ct, 16, cipherText, 0, ct.Length - 16);

            DeriveKeyAndIv(passphrase, salt);

            byte[] decrypted;
            using (var aes = new RijndaelManaged())
            {
                aes.BlockSize = BlockSize * 8;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var msDecrypt = new MemoryStream();
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write);
                csDecrypt.Write(cipherText, 0, cipherText.Length);
                csDecrypt.FlushFinalBlock();
                decrypted = msDecrypt.ToArray();
            }

            return decrypted;
        }

        /// <summary>
        /// Derive key and iv.
        /// </summary>
        /// <param name="passphrase">Passphrase</param>
        /// <param name="salt">Salt</param>
        protected void DeriveKeyAndIv(string passphrase, byte[] salt)
        {
            MD5 md5 = MD5.Create();

            key = new byte[KeyLen];
            iv = new byte[IvLen];

            byte[] dx = Array.Empty<byte>();
            byte[] salted = Array.Empty<byte>();
            byte[] pass = Encoding.UTF8.GetBytes(passphrase);

            for (int i = 0; i < (KeyLen + IvLen / 16); i++)
            {
                dx = Concat(Concat(dx, pass), salt);
                dx = md5.ComputeHash(dx);
                salted = Concat(salted, dx);
            }
            Array.Copy(salted, 0, key, 0, KeyLen);
            Array.Copy(salted, KeyLen, iv, 0, IvLen);
        }

        /// <summary>
        /// Concatenates two byte arrays.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static byte[] Concat(byte[] a, byte[] b)
        {
            byte[] output = new byte[a.Length + b.Length];
            for (int i = 0; i < a.Length; i++)
            {
                output[i] = a[i];
            }
            for (int j = 0; j < b.Length; j++)
            {
                output[a.Length + j] = b[j];
            }
            return output;
        }

        /// <summary>
        /// Concatenates a string with a byte array.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static byte[] Concat(string a, byte[] b)
        {
            return Concat(Encoding.UTF8.GetBytes(a), b);
        }
    }
}
