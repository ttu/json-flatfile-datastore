using System.IO;

namespace JsonFlatFileDataStore.Test;

public class FileAccessTests
{
    // WriteJsonToFile — catch (Exception) return false path
    [Fact]
    public void WriteJsonToFile_DirectoryDoesNotExist_ReturnsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}", "file.json");
        var result = FileAccess.WriteJsonToFile(path, s => s, "{}");
        Assert.False(result);
    }

    [Fact]
    public void WriteJsonToFile_ValidPath_ReturnsTrue()
    {
        var path = UTHelpers.GetFullFilePath($"FAWrite_{DateTime.UtcNow.Ticks}");
        var result = FileAccess.WriteJsonToFile(path, s => s, "{\"test\":1}");
        Assert.True(result);
        Assert.Equal("{\"test\":1}", File.ReadAllText(path));
        UTHelpers.Down(path);
    }

    [Fact]
    public void WriteJsonToFile_EncryptTransformApplied()
    {
        var path = UTHelpers.GetFullFilePath($"FAEnc_{DateTime.UtcNow.Ticks}");
        var result = FileAccess.WriteJsonToFile(path, s => s + "SUFFIX", "{}");
        Assert.True(result);
        Assert.Equal("{}SUFFIX", File.ReadAllText(path));
        UTHelpers.Down(path);
    }

    // ReadJsonFromFile — FileNotFoundException creates file
    [Fact]
    public void ReadJsonFromFile_FileNotFound_CreatesFileAndReturnsEmptyObject()
    {
        var path = UTHelpers.GetFullFilePath($"FARead_{DateTime.UtcNow.Ticks}");
        Assert.False(File.Exists(path));

        var content = FileAccess.ReadJsonFromFile(path, s => s, s => s);

        Assert.True(File.Exists(path));
        Assert.Equal("{}", content);
        UTHelpers.Down(path);
    }

    [Fact]
    public void ReadJsonFromFile_ExistingFile_ReturnsContent()
    {
        var path = UTHelpers.GetFullFilePath($"FAReadExist_{DateTime.UtcNow.Ticks}");
        File.WriteAllText(path, "{\"key\":42}");

        var content = FileAccess.ReadJsonFromFile(path, s => s, s => s);

        Assert.Equal("{\"key\":42}", content);
        UTHelpers.Down(path);
    }

    [Fact]
    public void ReadJsonFromFile_DecryptTransformApplied()
    {
        var path = UTHelpers.GetFullFilePath($"FADecrypt_{DateTime.UtcNow.Ticks}");
        File.WriteAllText(path, "ENCRYPTED");

        var content = FileAccess.ReadJsonFromFile(path, s => s, s => "DECRYPTED");

        Assert.Equal("DECRYPTED", content);
        UTHelpers.Down(path);
    }
}