using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace JsonFlatFileDataStore.Test
{
    public static class UTHelpers
    {
        public static string Up([CallerMemberName] string name = "")
        {
            var dir = Path.GetDirectoryName(typeof(DataStoreTests).GetTypeInfo().Assembly.Location);

            var path = Path.Combine(dir, "datastore.json");
            var content = File.ReadAllText(path);

            var newFilePath = Path.Combine(dir, $"{name}.json");
            File.WriteAllText(newFilePath, content);

            return newFilePath;
        }

        public static void Down(string fullPath)
        {
            File.Delete(fullPath);
        }
    }
}