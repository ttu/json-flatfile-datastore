using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class CollectionTests
    {
        [Fact]
        public void Find_And_AsQueryable()
        {
            var newFilePath = UTHelpers.Up();

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

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void GetNextIdValue_id()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            var lastItem = collection
                            .AsQueryable()
                            .OrderBy(e => e.id)
                            .Last();

            Assert.Equal(3, lastItem.id);

            var nextId = collection.GetNextIdValue();
            Assert.Equal(4, nextId);

            collection.InsertOne(new { id = nextId });

            nextId = collection.GetNextIdValue();
            Assert.Equal(5, nextId);
        }

        [Fact]
        public void GetNextIdValue_string()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath, keyProperty: "helloField");

            var collection = store.GetCollection("collectionWithStringId");

            collection.InsertOne(new { helloField = "SomeValue" });

            var inserted = collection.AsQueryable().First();

            var nextId = collection.GetNextIdValue();
            Assert.Equal("SomeValue0", nextId);

            collection.InsertOne(new { helloField = nextId });

            nextId = collection.GetNextIdValue();
            Assert.Equal("SomeValue1", nextId);
        }

        [Fact]
        public void GetNextIdValue_string_empty()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath, keyProperty: "myId");

            var collection = store.GetCollection("collectionWithStringId");

            collection.InsertOne(new { myId = "" });

            var inserted = collection.AsQueryable().First();

            var nextId = collection.GetNextIdValue();
            Assert.Equal("0", nextId);

            collection.InsertOne(new { myId = nextId });

            nextId = collection.GetNextIdValue();
            Assert.Equal("1", nextId);
        }

        [Fact]
        public void GetNextIdValue_typed_User()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            var nextId = collection.GetNextIdValue();
            Assert.Equal(4, nextId);
        }

        [Fact]
        public void AsQueryable_ComplicatedModel_Dynamic_And_Typed()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var itemDynamic = store.GetCollection("family")
                                .AsQueryable()
                                .Single(p => p.id == "AndersenFamily");

            var itemTyped = store.GetCollection<Family>()
                              .Find(p => p.Id == "AndersenFamily")
                              .First();

            Assert.Equal("Thomas", itemDynamic.parents[0].firstName);
            Assert.Equal("Thomas", itemTyped.Parents[0].FirstName);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void InsertOne_Dynamic()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            Assert.Equal(3, collection.Count);

            collection.InsertOne(new { id = 16, name = "Teddy" });
            Assert.Equal(4, collection.Count);

            var collection2 = store.GetCollection("user");
            Assert.Equal(4, collection2.Count);

            var store2 = new DataStore(newFilePath);

            var collection3 = store2.GetCollection("user");
            Assert.Equal(4, collection3.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void InsertOne_User()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            collection.InsertOne(new User { Id = 12, Name = "Teddy" });
            Assert.Equal(4, collection.Count);

            var collection2 = store.GetCollection("user");
            Assert.Equal(4, collection2.Count);

            var store2 = new DataStore(newFilePath);

            var collection3 = store2.GetCollection("user");
            Assert.Equal(4, collection3.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task InsertOneAsync_User()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            await collection.InsertOneAsync(new User { Id = 24, Name = "Teddy" });
            Assert.Equal(4, collection.Count);

            var collection2 = store.GetCollection("user");
            Assert.Equal(4, collection2.Count);

            var store2 = new DataStore(newFilePath);

            var collection3 = store2.GetCollection("user");
            Assert.Equal(4, collection3.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void UpdateOne_User()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            collection.InsertOne(new User { Id = 11, Name = "Teddy", Age = 21 });
            Assert.Equal(4, collection.Count);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection<User>("user");
            Assert.Equal(4, collection2.Count);

            collection2.UpdateOne(e => e.Id == 11, new { Age = 22 });

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<User>("user");
            var updated = collection3.Find(e => e.Id == 11).First();
            Assert.Equal(22, updated.Age);
            Assert.Equal("Teddy", updated.Name);

            // Try to update property that doesn't exist
            collection2.UpdateOne(e => e.Id == 11, new { SomeThatIsNotThere = "No" });

            UTHelpers.Down(newFilePath);
        }    

        [Fact]
        public void ReplaceOne_User()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            collection.InsertOne(new User { Id = 11, Name = "Teddy" });
            Assert.Equal(4, collection.Count);

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection<User>("user");
            Assert.Equal(4, collection2.Count);

            collection2.ReplaceOne(e => e.Id == 11, new User { Id = 11, Name = "Theodor" });

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<User>("user");
            var updated = collection3.Find(e => e.Id == 11).First();
            Assert.Equal("Theodor", updated.Name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void DeleteOne_User()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            collection.InsertOne(new User { Id = 11, Name = "Teddy" });
            Assert.Equal(4, collection.Count);

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection<User>("user");
            collection2.DeleteOne(e => e.Id == 11);
            Assert.Equal(3, collection2.Count);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<User>("user");
            Assert.Equal(3, collection3.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task DeleteOneAsync_NewCollection()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath, false);

            var collection = store.GetCollection("book");
            Assert.Equal(0, collection.Count);

            await collection.InsertOneAsync(new { Id = 1, Name = "Some name" });
            Assert.Equal(1, collection.Count);

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection("book");
            Assert.Equal(1, collection2.Count);

            await collection2.DeleteOneAsync(e => e.Id == 1);
            Assert.Equal(0, collection2.Count);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection("book");
            Assert.Equal(0, collection3.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task InsertOne_100_Sync_Test_Threaded()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collectionPreCheck = store.GetCollection<User>("user");
            Assert.Equal(3, collectionPreCheck.Count);

            var tasks = Enumerable.Range(0, 2).Select(cId =>
            {
                return Task.Run<bool>(() =>
                {
                    var collection = store.GetCollection<User>($"user_{cId}");

                    for (int i = 0; i < 50; i++)
                    {
                        collection.InsertOne(new User { Id = i, Name = "Teddy" });
                    }

                    return true;
                });
            });

            await Task.WhenAll(tasks);

            var store2 = new DataStore(newFilePath);
            var collection2 = store.GetCollection<User>("user");
            Assert.Equal(3, collection2.Count);
            var collection_0 = store.GetCollection<User>("user_0");
            Assert.Equal(50, collection_0.Count);
            var collection_1 = store.GetCollection<User>("user_1");
            Assert.Equal(50, collection_1.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task InsertOne_100_Async_Test()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            var tasks = Enumerable.Range(0, 100).Select(i => collection.InsertOneAsync(new User { Id = i, Name = "Teddy" }));

            await Task.WhenAll(tasks);

            var store2 = new DataStore(newFilePath);
            var collection2 = store.GetCollection<User>("user");
            Assert.Equal(103, collection2.Count);

            UTHelpers.Down(newFilePath);
        }
    }
}