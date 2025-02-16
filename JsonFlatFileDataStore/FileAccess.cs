using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace JsonFlatFileDataStore
{
    public interface IStorageAccess
    {
        string ReadJson(string path, Func<string, string> encryptJson, Func<string, string> decryptJson);
        bool WriteJson(string path, Func<string, string> encryptJson, string content);
    }

    public static class StorageAccess
    {
        private static IStorageAccess _storageAccess;

        public static IStorageAccess GetStorageAccess()
        {
            _storageAccess ??= new FileStorage();
            return _storageAccess;
        }

        public static void SetStorageAccess(IStorageAccess storageAccess)
        {
            _storageAccess = storageAccess;
        }
    }

    public class FileStorage : IStorageAccess
    {
        public string ReadJson(string path, Func<string, string> encryptJson, Func<string, string> decryptJson)
        {
            Stopwatch sw = null;
            var json = "{}";

            while (true)
            {
                try
                {
                    json = File.ReadAllText(path);
                    break;
                }
                catch (FileNotFoundException)
                {
                    json = encryptJson(json);
                    File.WriteAllText(path, json);
                    break;
                }
                catch (IOException e) when (e.Message.Contains("because it is being used by another process"))
                {
                    // If some other process is using this file, retry operation unless elapsed times is greater than 10sec
                    sw ??= Stopwatch.StartNew();
                    if (sw.ElapsedMilliseconds > 10000)
                        throw;
                }
            }

            return decryptJson(json);
        }

        public bool WriteJson(string path, Func<string, string> encryptJson, string content)
        {
            Stopwatch sw = null;

            while (true)
            {
                try
                {
                    File.WriteAllText(path, encryptJson(content));
                    return true;
                }
                catch (IOException e) when (e.Message.Contains("because it is being used by another process"))
                {
                    // If some other process is using this file, retry operation unless elapsed times is greater than 10sec
                    sw ??= Stopwatch.StartNew();
                    if (sw.ElapsedMilliseconds > 10000)
                        return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }

    public class InMemoryStorage : IStorageAccess
    {
        private string _content = "{}";

        public string ReadJson(string path, Func<string, string> encryptJson, Func<string, string> decryptJson) => _content;

        public bool WriteJson(string path, Func<string, string> encryptJson, string content)
        {
            _content = content;
            return true;
        }
    }
}