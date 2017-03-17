using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class DataStoreTests
    {
        private class User
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public int Age { get; set; }

            public string Location { get; set; }
        }

        private class Movie
        {
            public string Name { get; set; }
        }

        private class Family
        {
            public string Id { get; set; }

            public List<Parent> Parents { get; set; }
        }

        private class Parent
        {
            public string FirstName { get; set; }
        }

        private string Up([CallerMemberName] string name = "")
        {
            var dir = Path.GetDirectoryName(typeof(DataStoreTests).GetTypeInfo().Assembly.Location);

            var path = Path.Combine(dir, "datastore.json");
            var content = File.ReadAllText(path);

            var newFilePath = Path.Combine(dir, $"{name}.json");
            File.WriteAllText(newFilePath, content);

            return newFilePath;
        }

        private void Down(string fullPath)
        {
            File.Delete(fullPath);
        }

        [Fact]
        public void ListCollections()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var collections = store.ListCollections();
            Assert.Equal("user", collections.First());
            Assert.Equal(3, collections.Count());

            Down(newFilePath);
        }

        [Fact]
        public void Find_And_AsQueryable()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var itemDynamic = store.GetCollection("user")
                                .AsQueryable()
                                .Single(p => p.name == "Phil");

            var itemTyped1 = store.GetCollection<User>()
                                .AsQueryable()
                                .Single(p => p.Name == "Phil");

            var itemTyped2 = store.GetCollection<User>()
                                .Find(p => p.Name == "Phil")
                                .First();

            Assert.Equal("Phil", itemDynamic.name);
            Assert.Equal("Phil", itemTyped1.Name);
            Assert.Equal("Phil", itemTyped2.Name);

            Down(newFilePath);
        }

        [Fact]
        public void AsQueryable_ComplicatedModel_Dynamic_And_Typed()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var itemDynamic = store.GetCollection("family")
                                .AsQueryable()
                                .Single(p => p.id == "AndersenFamily");

            var itemTyped = store.GetCollection<Family>()
                              .Find(p => p.Id == "AndersenFamily")
                              .First();

            Assert.Equal("Thomas", itemDynamic.parents[0].firstName);
            Assert.Equal("Thomas", itemTyped.Parents[0].FirstName);

            Down(newFilePath);
        }

        [Fact]
        public void InsertOne_Dynamic()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            Assert.Equal(3, collection.Count);

            collection.InsertOne(new { id = "acac", name = "Teddy" });
            Assert.Equal(4, collection.Count);

            var collection2 = store.GetCollection("user");
            Assert.Equal(4, collection2.Count);

            var store2 = new DataStore(newFilePath);

            var collection3 = store2.GetCollection("user");
            Assert.Equal(4, collection3.Count);

            Down(newFilePath);
        }

        [Fact]
        public void InsertOne_User()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            collection.InsertOne(new User { Id = "acac", Name = "Teddy" });
            Assert.Equal(4, collection.Count);

            var collection2 = store.GetCollection("user");
            Assert.Equal(4, collection2.Count);

            var store2 = new DataStore(newFilePath);

            var collection3 = store2.GetCollection("user");
            Assert.Equal(4, collection3.Count);

            Down(newFilePath);
        }

        [Fact]
        public async Task InsertOneAsync_User()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            await collection.InsertOneAsync(new User { Id = "acac", Name = "Teddy" });
            Assert.Equal(4, collection.Count);

            var collection2 = store.GetCollection("user");
            Assert.Equal(4, collection2.Count);

            var store2 = new DataStore(newFilePath);

            var collection3 = store2.GetCollection("user");
            Assert.Equal(4, collection3.Count);

            Down(newFilePath);
        }

        [Fact]
        public void ReplaceOne_User()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            collection.InsertOne(new User { Id = "acac", Name = "Teddy" });
            Assert.Equal(4, collection.Count);

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection<User>("user");
            Assert.Equal(4, collection2.Count);

            collection2.ReplaceOne(e => e.Id == "acac", new User { Id = "acac", Name = "Theodor" });

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<User>("user");
            var updated = collection3.Find(e => e.Id == "acac").First();
            Assert.Equal("Theodor", updated.Name);

            Down(newFilePath);
        }

        [Fact]
        public void DeleteOne_User()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            collection.InsertOne(new User { Id = "acac", Name = "Teddy" });
            Assert.Equal(4, collection.Count);

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection<User>("user");
            collection2.DeleteOne(e => e.Id == "acac");
            Assert.Equal(3, collection2.Count);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<User>("user");
            Assert.Equal(3, collection3.Count);

            Down(newFilePath);
        }

        [Fact]
        public async Task DeleteOneAsync_NewCollection()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("book");
            Assert.Equal(0, collection.Count);

            await collection.InsertOneAsync(new { Id = "1", Name = "Some name" });
            Assert.Equal(1, collection.Count);

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection("book");
            Assert.Equal(1, collection2.Count);

            await collection2.DeleteOneAsync(e => e.Id == "1");
            Assert.Equal(0, collection2.Count);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection("book");
            Assert.Equal(0, collection3.Count);

            Down(newFilePath);
        }

        [Fact]
        public void InsertOne_100_Sync_Test()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            for (int i = 0; i < 100; i++)
            {
                collection.InsertOne(new User { Id = i.ToString(), Name = "Teddy" });
            }

            var store2 = new DataStore(newFilePath);
            var collection2 = store.GetCollection<User>("user");
            Assert.Equal(103, collection2.Count);

            Down(newFilePath);
        }

        [Fact]
        public async Task InsertOne_100_Async_Test()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            var tasks = Enumerable.Range(0, 100).Select(i => collection.InsertOneAsync(new User { Id = i.ToString(), Name = "Teddy" }));

            await Task.WhenAll(tasks);

            var store2 = new DataStore(newFilePath);
            var collection2 = store.GetCollection<User>("user");
            Assert.Equal(103, collection2.Count);

            Down(newFilePath);
        }

        [Fact]
        public void TypedCollection_Different_Name()
        {
            var newFilePath = Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<Movie>("movies");
            Assert.Equal(2, collection.Count);
            Assert.Equal(1, collection.AsQueryable().Count(e => e.Name == "Predator"));

            Down(newFilePath);
        }

        [Fact]
        public async Task Readme_Example()
        {
            var pathToJson = Up();

            var store = new DataStore(pathToJson);

            var dynamicCollection = store.GetCollection("user");

            var userDynamic = dynamicCollection
                                .AsQueryable()
                                .Single(p => p.name == "Phil");

            await dynamicCollection.InsertOneAsync(new { id = "4", name = "Raymond", age = 32 });
            await dynamicCollection.ReplaceOneAsync(e => e.id == "4", new { id = "2", name = "Barry", age = 32 });
            await dynamicCollection.DeleteOneAsync(e => e.name == "Barry");

            var typedCollection = store.GetCollection<User>();

            var userTyped = typedCollection
                                .AsQueryable()
                                .Single(p => p.Name == "Phil");

            typedCollection.InsertOne(new User { Id = "5", Name = "Jim", Age = 52 });
            typedCollection.ReplaceOne(e => e.Id == "5", new User { Id = "3", Name = "Barry", Age = 52 });
            typedCollection.DeleteOne(e => e.Name == "Barry");
            typedCollection.DeleteMany(e => e.Age < 31);

            Assert.Equal("Phil", userDynamic.name);
            Assert.Equal("Phil", userTyped.Name);

            Down(pathToJson);
        }
    }
}