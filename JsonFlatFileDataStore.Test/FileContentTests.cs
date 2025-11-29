using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class FileContentTests
    {
        [Fact]
        public void FileNotFound_CreateNewFile()
        {
            var path = UTHelpers.GetFullFilePath($"CreateNewFile_{DateTime.UtcNow.Ticks}");

            var storeFileNotFound = new DataStore(path);
            var collectionKeys = storeFileNotFound.GetKeys();
            Assert.Empty(collectionKeys);

            var storeFileFound = new DataStore(path);
            var collectionKeysFileFound = storeFileFound.GetKeys();
            Assert.Empty(collectionKeysFileFound);

            UTHelpers.Down(path);
        }

        [Theory]
        [InlineData(true, true, new[] { 40 })]
        [InlineData(false, true, new[] { 40 })]
        [InlineData(true, false, new[] { 81, 74 })]
        [InlineData(false, false, new[] { 81, 74 })]
        public async Task AllFormats_CorrectLength(bool useLowerCamelCase, bool useMinifiedJson, int[] allowedLengths)
        {
            var path = UTHelpers.GetFullFilePath($"AllFormats_CorrectLength_{DateTime.UtcNow.Ticks}");

            var store = new DataStore(path, useLowerCamelCase: useLowerCamelCase, minifyJson: useMinifiedJson);
            var collection = store.GetCollection<Movie>("movie");
            await collection.InsertOneAsync(new Movie { Name = "Test", Rating = 5 });

            var content = UTHelpers.GetFileContent(path);

            // NOTE: File format is different depending on used OS. Windows uses \r\n and Linux/macOS \r
            //   - "{\r\n  \"movie\": [\r\n    {\r\n      \"name\": \"Test\",\r\n      \"rating\": 5.0\r\n    }\r\n  ]\r\n}",
            //   - "{\r  \"movie\": [\r    {\r      \"name\": \"Test\",\r      \"rating\": 5.0\r    }\r  ]\r}"
            // Length on Windows is 81 and on Linux/macOS 74
            //
            // Minified length: 40
            //  - "{\"movie\":\"name\":\"Test\",\"rating\":5.0}]}"

            Assert.Contains(allowedLengths, i => i == content.Length);

            UTHelpers.Down(path);
        }

        [Fact]
        public void Encrypted_AlwaysMinify()
        {
            var path = UTHelpers.GetFullFilePath($"Encrypted_AlwaysMinify_{DateTime.UtcNow.Ticks}");

            var storeFileEncrypted = new DataStore(path, encryptionKey: "53cr3t");
            storeFileEncrypted.InsertItem<Movie>("movie", new Movie { Name = "Matrix", Rating = 5 });
            var content = UTHelpers.GetFileContent(path);

            Assert.Equal(88, content.Length);

            UTHelpers.Down(path);
        }

        [Fact]
        public void Encrypted_FileNotFound_CreateNewFile()
        {
            var path = UTHelpers.GetFullFilePath($"Encrypted_FileNotFound_CreateNewFile_{DateTime.UtcNow.Ticks}");

            var storeFileNotFound = new DataStore(path, encryptionKey: "53cr3t");
            var collectionKeys = storeFileNotFound.GetKeys();
            Assert.Empty(collectionKeys);

            var storeFileFound = new DataStore(path, encryptionKey: "53cr3t");
            var collectionKeysFileFound = storeFileFound.GetKeys();
            Assert.Empty(collectionKeysFileFound);

            UTHelpers.Down(path);
        }

        [Fact]
        public void DynamicCollection_Has_Correct_PropertyNames()
        {
            var path = UTHelpers.GetFullFilePath($"DynamicCollection_Has_Correct_PropertyNames_{DateTime.UtcNow.Ticks}");

            var store = new DataStore(path);

            var collection = store.GetCollection("User");
            collection.InsertOne(new { id = 1, firstName = "Test" });
            var collection2 = store.GetCollection("User");
            collection2.InsertOne(new { id = 2, firstName = "Test2" });
            var collection3 = store.GetCollection("user");
            collection3.InsertOne(new { id = 3, firstName = "Test3" });

            // Verify collection name
            var content = UTHelpers.GetFileContent(path);
            var propCountLower = Regex.Matches(content, "user").Count;
            Assert.Equal(1, propCountLower);
            var propCountUpper = Regex.Matches(content, "User").Count;
            Assert.Equal(0, propCountUpper);

            // Verify property name
            Assert.Contains("firstName", content);
            Assert.DoesNotContain("FirstName", content);

            // Verify collection item count
            var assertCollectionLower = store.GetCollection("user");
            Assert.Equal(3, assertCollectionLower.Count);

            var assertCollectionUpper = store.GetCollection("User");
            Assert.Equal(3, assertCollectionUpper.Count);
        }

        [Fact]
        public void TypedCollection_Has_Correct_PropertyNames()
        {
            var path = UTHelpers.GetFullFilePath($"TypedCollection_Has_Correct_PropertyNames_{DateTime.UtcNow.Ticks}");

            var store = new DataStore(path);

            var collection = store.GetCollection<Employee>();
            collection.InsertOne(new Employee { Id = 1, FirstName = "first" });
            var collection2 = store.GetCollection("Employee");
            collection2.InsertOne(new Employee { Id = 2, FirstName = "second" });
            var collection3 = store.GetCollection("employee");
            collection3.InsertOne(new Employee { Id = 3, FirstName = "third" });

            // Verify collection name
            var content = UTHelpers.GetFileContent(path);
            var propCountLower = Regex.Matches(content, "employee").Count;
            Assert.Equal(1, propCountLower);
            var propCountUpper = Regex.Matches(content, "Employee").Count;
            Assert.Equal(0, propCountUpper);

            // Verify property name
            Assert.Contains("firstName", content);
            Assert.DoesNotContain("FirstName", content);

            // Verify collection item count
            var assertCollection = store.GetCollection<Employee>();
            Assert.Equal(3, assertCollection.Count);

            var assertCollectionUpper = store.GetCollection<Employee>("Employee");
            Assert.Equal(3, assertCollectionUpper.Count);

            var assertCollectionLower = store.GetCollection<Employee>("employee");
            Assert.Equal(3, assertCollectionLower.Count);
        }

        [Fact]
        public void SingleItem_Has_Correct_PropertyNames()
        {
            var path = UTHelpers.GetFullFilePath($"SingleItem_Has_Correct_PropertyNames_{DateTime.UtcNow.Ticks}");

            var store = new DataStore(path);

            store.ReplaceItem("TestOkIsThis1", 1, true);
            store.ReplaceItem("testOkIsThis2", 2, true); // Insert with different case
            store.ReplaceItem("TestOkIsThis2", 3, true);

            var content = UTHelpers.GetFileContent(path);
            var propCount = Regex.Matches(content, "testOkIsThis2").Count;
            Assert.Equal(1, propCount);

            var itemUpper = store.GetItem("TestOkIsThis2");
            Assert.Equal(3, itemUpper);

            var itemLower = store.GetItem("testOkIsThis2");
            Assert.Equal(3, itemLower);
        }

        [Fact]
        public async Task WriteToFile_LowerCamelCase()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<PrivateOwner>("PrivateOwner");
            Assert.Equal(0, collection.Count);

            await collection.InsertOneAsync(new PrivateOwner { FirstName = "Jimmy", OwnerLongTestProperty = "UT" });
            Assert.Equal(1, collection.Count);

            var json = File.ReadAllText(newFilePath);

            Assert.Contains("privateOwner", json);
            Assert.Contains("ownerLongTestProperty", json);

            var store2 = new DataStore(newFilePath);

            var collectionUppercase = store2.GetCollection<PrivateOwner>("PrivateOwner");
            Assert.Equal(1, collectionUppercase.Count);

            var collectionLowercase = store2.GetCollection<PrivateOwner>("privateOwner");
            Assert.Equal(1, collectionLowercase.Count);

            var collectionNoCase = store2.GetCollection<PrivateOwner>();
            Assert.Equal(1, collectionNoCase.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task WriteToFile_UpperCamelCase()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath, false);

            var collection = store.GetCollection<PrivateOwner>("Owner");
            Assert.Equal(0, collection.Count);

            await collection.InsertOneAsync(new PrivateOwner { FirstName = "Jimmy", OwnerLongTestProperty = "UT" });
            Assert.Equal(1, collection.Count);

            var json = File.ReadAllText(newFilePath);

            Assert.Contains("OwnerLongTestProperty", json);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task DynamicCollection_UpdateWrongCase_NoDuplicateProperties()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            Assert.Equal(3, collection.Count);

            await collection.InsertOneAsync(new { id = 11, name = "Teddy", age = 21 });

            // Update with wrong case - capital A in Age
            dynamic source = new ExpandoObject();
            source.Age = 22;
            var updateResult = await collection.UpdateOneAsync(e => e.id == 11, source as object);
            Assert.True(updateResult);

            // Read the raw JSON and verify no duplicate properties with different casing
            var json = File.ReadAllText(newFilePath);
            var ageCountLower = Regex.Matches(json, "\"age\"").Count;
            var ageCountUpper = Regex.Matches(json, "\"Age\"").Count;

            // Property 'age' (lowercase) should exist in the JSON
            Assert.True(ageCountLower > 0);
            // Property 'Age' (uppercase) should NOT exist - no duplicate properties with different casing
            Assert.Equal(0, ageCountUpper);

            // Verify the value is correct
            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection("user");
            var updated = collection2.Find(e => e.id == 11).First();
            Assert.Equal(22, updated.age);
            Assert.Equal("Teddy", updated.name);

            UTHelpers.Down(newFilePath);
        }

        public class Employee
        {
            public int Id { get; set; }

            public string FirstName { get; set; }

            public int Age { get; set; }
        }
    }
}