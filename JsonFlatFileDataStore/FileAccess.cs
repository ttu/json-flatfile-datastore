using System;
using System.Diagnostics;
using System.IO;

namespace JsonFlatFileDataStore
{
    internal static class FileAccess
    {
        internal static string ReadJsonFromFile(string path, Func<string, string> encryptJson, Func<string, string> decryptJson)
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

        internal static bool WriteJsonToFile(string path, Func<string, string> encryptJson, string content)
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
}