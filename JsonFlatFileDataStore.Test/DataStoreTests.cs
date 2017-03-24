using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class DataStoreTests
    {
        [Fact]
        public void ListCollections()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collections = store.ListCollections();
            Assert.Equal("user", collections.First());
            Assert.Equal(3, collections.Count());

            UTHelpers.Down(newFilePath);
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

            Assert.True(json.Contains("privateOwner"));
            Assert.True(json.Contains("ownerLongTestProperty"));

            var store2 = new DataStore(newFilePath);

            var collectionUppercase = store2.GetCollection<PrivateOwner>("PrivateOwner");
            Assert.Equal(0, collectionUppercase.Count);

            var collectionLowercase = store2.GetCollection<PrivateOwner>("privateOwner");
            Assert.Equal(1, collectionLowercase.Count);

            var collectionNocase = store2.GetCollection<PrivateOwner>();
            Assert.Equal(1, collectionNocase.Count);

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

            Assert.True(json.Contains("OwnerLongTestProperty"));

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void GetCollection_TypedCollection_NameParameter()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<Movie>("movies");
            Assert.Equal(2, collection.Count);
            Assert.Equal(1, collection.AsQueryable().Count(e => e.Name == "Predator"));

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task Readme_Example()
        {
            var pathToJson = UTHelpers.Up();

            var store = new DataStore(pathToJson);

            var dynamicCollection = store.GetCollection("user");

            var userDynamic = dynamicCollection
                                .AsQueryable()
                                .Single(p => p.name == "Phil");

            await dynamicCollection.InsertOneAsync(new { id = 14, name = "Raymond", age = 32 });
            await dynamicCollection.ReplaceOneAsync(e => e.id == 14, new { id = 14, name = "Barry", age = 32 });
            await dynamicCollection.DeleteOneAsync(e => e.name == "Barry");

            var typedCollection = store.GetCollection<User>();

            var userTyped = typedCollection
                                .AsQueryable()
                                .Single(p => p.Name == "Phil");

            typedCollection.InsertOne(new User { Id = 15, Name = "Jim", Age = 52 });
            typedCollection.ReplaceOne(e => e.Id == 15, new User { Id = 15, Name = "Barry", Age = 52 });
            typedCollection.DeleteOne(e => e.Name == "Barry");
            typedCollection.DeleteMany(e => e.Age < 31);

            Assert.Equal("Phil", userDynamic.name);
            Assert.Equal("Phil", userTyped.Name);

            UTHelpers.Down(pathToJson);
        }
    }
}