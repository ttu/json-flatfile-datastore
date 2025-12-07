using System.Diagnostics;
using System.IO;

namespace JsonFlatFileDataStore;

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

    /// <summary>
    /// Writes JSON content to a file using an atomic write-and-replace pattern.
    /// This prevents race conditions where concurrent readers might read partial/malformed JSON during writes.
    ///
    /// Race condition scenario:
    /// - Thread A: Writes to file (File.WriteAllText) - takes time to write all bytes
    /// - Thread B: Reads file mid-write (File.ReadAllText) - gets incomplete JSON
    /// - Thread B: JsonDocument.Parse() fails with "Expected depth to be zero" error
    ///
    /// This can occur in:
    /// - Multiple DataStore instances with reloadBeforeGetCollection: true
    /// - Multi-process scenarios sharing the same datastore file
    /// - High write concurrency with concurrent reads
    ///
    /// Solution: Write to temporary file first, then atomically replace the target file.
    /// File.Replace() ensures readers see either the complete old content or complete new content, never partial writes.
    /// </summary>
    internal static bool WriteJsonToFile(string path, Func<string, string> encryptJson, string content)
    {
        Stopwatch sw = null;

        while (true)
        {
            try
            {
                // Use atomic write-and-replace pattern to prevent readers from seeing partial JSON
                // Write to a temporary file first, then atomically replace the target file
                var tempPath = path + ".tmp";
                File.WriteAllText(tempPath, encryptJson(content));

                // Atomically replace the target file with the temp file
                if (File.Exists(path))
                {
                    try
                    {
                        // File.Replace is atomic and creates a backup
                        // Note: Requires files to be on the same volume and may not work on all filesystems
                        var backupPath = path + ".bak";
                        File.Replace(tempPath, path, backupPath);
                        // Clean up the backup file
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // Fallback for filesystems that don't support File.Replace (e.g., some network shares)
                        // This is not atomic but better than failing completely
                        File.Delete(path);
                        File.Move(tempPath, path);
                    }
                    catch (UnauthorizedAccessException ex) when (ex.Message.Contains("not supported"))
                    {
                        // Fallback for platforms/filesystems that don't support File.Replace
                        File.Delete(path);
                        File.Move(tempPath, path);
                    }
                }
                else
                {
                    // Target file doesn't exist yet, just move the temp file
                    File.Move(tempPath, path);
                }
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