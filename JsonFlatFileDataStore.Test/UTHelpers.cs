using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace JsonFlatFileDataStore.Test
{
    public static class UTHelpers
    {
        private static readonly string _dir = Path.GetDirectoryName(typeof(DataStoreTests).GetTypeInfo().Assembly.Location);
        private static readonly Aes256 _aes256 = new Aes256();

        private static readonly Lazy<string> _originalContent = new Lazy<string>(() =>
        {
            var path = Path.Combine(_dir, "datastore.json");
            return File.ReadAllText(path);
        });

        public static string Up([CallerMemberName] string name = "", string encryptionKey = null)
        {
            var newFilePath = Path.Combine(_dir, $"{name}.json");
            var dbContent = string.IsNullOrEmpty(encryptionKey) ? _originalContent.Value : _aes256.Encrypt(_originalContent.Value, encryptionKey);
            File.WriteAllText(newFilePath, dbContent);
            return newFilePath;
        }

        public static void Down(string fullPath)
        {
            File.Delete(fullPath);
        }
    }
}