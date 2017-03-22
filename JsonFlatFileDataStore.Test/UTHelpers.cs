using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace JsonFlatFileDataStore.Test
{
    internal static class UTHelpers
    {
        internal static string Up([CallerMemberName] string name = "")
        {
            var dir = Path.GetDirectoryName(typeof(DataStoreTests).GetTypeInfo().Assembly.Location);

            var path = Path.Combine(dir, "datastore.json");
            var content = File.ReadAllText(path);

            var newFilePath = Path.Combine(dir, $"{name}.json");
            File.WriteAllText(newFilePath, content);

            return newFilePath;
        }

        internal static void Down(string fullPath)
        {
            File.Delete(fullPath);
        }
    }
}