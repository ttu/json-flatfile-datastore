// Consumer app for scripts/smoke-test-package.sh. Copied into a throwaway console
// project that references JsonFlatFileDataStore as a NuGet package, not a project
// reference, so it exercises the packaged assembly the way a real user would.
//
// This is a packaging smoke test, not a functional test suite: it touches each
// public surface once and checks persistence survives a store round-trip. Deep
// behavior coverage belongs in JsonFlatFileDataStore.Test.

using System.Dynamic;
using JsonFlatFileDataStore;

var failures = 0;

void Check(string name, bool condition)
{
    if (condition)
    {
        Console.WriteLine($"    ok  {name}");
    }
    else
    {
        Console.WriteLine($"    FAIL {name}");
        failures++;
    }
}

var dir = Path.Combine(Path.GetTempPath(), "jffds-smoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(dir);

try
{
    // Typed collection CRUD, then reopen the file to prove writes were flushed on dispose.
    var typedPath = Path.Combine(dir, "typed.json");
    using (var store = new DataStore(typedPath))
    {
        var collection = store.GetCollection<Customer>("customer");
        collection.InsertOne(new Customer { Name = "Alice", Age = 30 });
        collection.InsertOne(new Customer { Name = "Bob", Age = 40 });

        Check("insert assigns incrementing ids", collection.AsQueryable().Select(c => c.Id).SequenceEqual(new[] { 0, 1 }));
        Check("find by predicate", collection.Find(c => c.Age > 35).Single().Name == "Bob");
        Check("full text search", collection.Find("alice").Single().Age == 30);

        collection.UpdateOne(0, new { Age = 31 });
        Check("partial update keeps other fields", collection.Find(c => c.Id == 0).Single() is { Age: 31, Name: "Alice" });

        collection.ReplaceOne(1, new Customer { Id = 1, Name = "Bobby", Age = 41 });
        Check("replace", collection.Find(c => c.Id == 1).Single().Name == "Bobby");

        Check("delete", collection.DeleteOne(c => c.Id == 1) && collection.AsQueryable().Count() == 1);
    }

    using (var store = new DataStore(typedPath))
    {
        Check("data persists across store instances", store.GetCollection<Customer>("customer").Find(c => c.Id == 0).Single().Age == 31);
    }

    // camelCase is the default, so the file on disk must use camelCase keys.
    var json = File.ReadAllText(typedPath);
    Check("camelCase serialization", json.Contains("\"customer\"") && json.Contains("\"name\"") && !json.Contains("\"Name\""));

    // Dynamic collections depend on Microsoft.CSharp flowing through as a package dependency.
    var dynamicPath = Path.Combine(dir, "dynamic.json");
    using (var store = new DataStore(dynamicPath))
    {
        var collection = store.GetCollection("item");
        dynamic item = new ExpandoObject();
        item.name = "widget";
        item.price = 9.99;
        collection.InsertOne(item);

        Check("dynamic insert and find", collection.Find(i => i.name == "widget").Single().price == 9.99);
        collection.UpdateOne(0, new { price = 12.5 });
        Check("dynamic update", collection.AsQueryable().Single().price == 12.5);
    }

    // Single-item API.
    var itemPath = Path.Combine(dir, "single.json");
    using (var store = new DataStore(itemPath))
    {
        store.InsertItem("config", new Config { Theme = "dark", Retries = 3 });
        Check("single item insert and get", store.GetItem<Config>("config").Theme == "dark");
        store.UpdateItem("config", new { Retries = 5 });
        Check("single item partial update", store.GetItem<Config>("config") is { Retries: 5, Theme: "dark" });
        Check("single item delete", store.DeleteItem("config") && store.GetKeys().Count == 0);
    }

    // Async path uses a different code route than the sync wrappers.
    var asyncPath = Path.Combine(dir, "async.json");
    using (var store = new DataStore(asyncPath))
    {
        var collection = store.GetCollection<Customer>("customer");
        await collection.InsertManyAsync(new[]
        {
            new Customer { Name = "Carol", Age = 25 },
            new Customer { Name = "Dave", Age = 35 }
        });
        Check("async insert many", collection.AsQueryable().Count() == 2);
        await collection.DeleteManyAsync(c => c.Age < 30);
        Check("async delete many", collection.AsQueryable().Single().Name == "Dave");
    }

    // Encryption writes ciphertext and reads it back through a fresh store.
    var securePath = Path.Combine(dir, "secure.json");
    using (var store = new DataStore(securePath, encryptionKey: "smoke-test-key"))
    {
        store.GetCollection<Customer>("customer").InsertOne(new Customer { Name = "Eve", Age = 50 });
    }

    Check("encrypted file is not plaintext", !File.ReadAllText(securePath).Contains("Eve"));

    using (var store = new DataStore(securePath, encryptionKey: "smoke-test-key"))
    {
        Check("encrypted round-trip", store.GetCollection<Customer>("customer").AsQueryable().Single().Name == "Eve");
    }
}
finally
{
    Directory.Delete(dir, true);
}

if (failures > 0)
{
    Console.WriteLine($"\n{failures} check(s) failed");
    return 1;
}

Console.WriteLine("\nAll consumer checks passed");
return 0;

public class Customer
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }
}

public class Config
{
    public string? Theme { get; set; }
    public int Retries { get; set; }
}
