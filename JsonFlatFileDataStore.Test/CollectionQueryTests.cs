using Newtonsoft.Json.Linq;
using System.Linq;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class CollectionQueryTests
    {
        [Fact]
        public void FindAndAsQueryable_Linq()
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
        public void GetNextIdValue_IntegerId()
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
        public void GetNextIdValue_StringType()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath, keyProperty: "helloField");

            var collection = store.GetCollection("collectionWithStringId");

            var shouldBeNone = collection.GetNextIdValue();
            Assert.Equal(0, shouldBeNone);

            collection.InsertOne(new { helloField = "SomeValue" });

            var inserted = collection.AsQueryable().First();

            var nextId = collection.GetNextIdValue();
            Assert.Equal("SomeValue0", nextId);

            collection.InsertOne(new { helloField = nextId });

            nextId = collection.GetNextIdValue();
            Assert.Equal("SomeValue1", nextId);
        }

        [Fact]
        public void GetNextIdValue_StringType_AnonymousType()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath, keyProperty: "myId");

            var collection = store.GetCollection("collectionWithStringId");

            collection.InsertOne(new { myId = "hello2" });

            var inserted = collection.AsQueryable().First();
            Assert.Equal("hello2", inserted.myId);

            var nextId = collection.GetNextIdValue();
            Assert.Equal("hello3", nextId);

            collection.InsertOne(new { myId = nextId });
            var item3 = collection.AsQueryable().FirstOrDefault(e => e.myId == nextId);
            Assert.Equal("hello3", item3.myId);

            nextId = collection.GetNextIdValue();
            Assert.Equal("hello4", nextId);

            collection.InsertOne(new { text = "myId missing" });

            var item4 = collection.AsQueryable().FirstOrDefault(e => e.myId == nextId);
            Assert.Equal("hello4", item4.myId);
            Assert.Equal("myId missing", item4.text);

            nextId = collection.GetNextIdValue();
            Assert.Equal("hello5", nextId);

            // This will insert item with hello2 as latest
            collection.InsertOne(new { myId = "hello2" });

            nextId = collection.GetNextIdValue();
            Assert.Equal("hello3", nextId);

            var same = collection.AsQueryable().Where(e => e.myId == "hello2");
            Assert.Equal(2, same.Count());
        }

        [Fact]
        public void GetNextIdValue_StringType_JToken()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath, keyProperty: "myId");

            var collection = store.GetCollection("collectionWithStringId");

            // Insert seed value with upsert
            collection.ReplaceOne(e => e, JToken.Parse("{ 'myId': 'test1' }"), true);

            var nextId = collection.GetNextIdValue();
            Assert.Equal("test2", nextId);

            var nextUpdate = JToken.Parse("{ 'myId': 'somethingWrong2' }");
            collection.InsertOne(nextUpdate);
            Assert.Equal(nextId, nextUpdate["myId"]);

            nextId = collection.GetNextIdValue();
            Assert.Equal("test3", nextId);

            nextUpdate = JToken.Parse("{ 'xxx': 111 }");
            collection.InsertOne(nextUpdate);
            Assert.Equal(nextId, nextUpdate["myId"]);
            Assert.Equal(111, nextUpdate["xxx"]);

            nextId = collection.GetNextIdValue();
            Assert.Equal("test4", nextId);
        }

        [Fact]
        public void GetNextIdValue_TypedUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            var nextId = collection.GetNextIdValue();
            Assert.Equal(4, nextId);
        }

        [Fact]
        public void AsQueryable_DynamicAndTypedComplicatedModel()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var itemDynamic = store.GetCollection("family")
                                .AsQueryable()
                                .Single(p => p.id == 12);

            var itemTyped = store.GetCollection<Family>()
                              .Find(p => p.Id == 12)
                              .First();

            Assert.Equal("Naomi", itemDynamic.parents[0].name);
            Assert.Equal("Naomi", itemTyped.Parents[0].Name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void Reload()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);
            var store2 = new DataStore(newFilePath);

            var collection1_1 = store.GetCollection("user");
            var collection2_1 = store2.GetCollection("user");
            Assert.Equal(3, collection1_1.Count);
            Assert.Equal(3, collection2_1.Count);

            collection1_1.InsertOne(new { id = 16, name = "Teddy" });
            Assert.Equal(4, collection1_1.Count);
            Assert.Equal(3, collection2_1.Count);

            var collection1_2 = store.GetCollection("user");
            var collection2_2 = store2.GetCollection("user");
            Assert.Equal(4, collection1_2.Count);
            Assert.Equal(3, collection2_2.Count);

            store2.Reload();

            var collection2_3 = store2.GetCollection("user");
            Assert.Equal(4, collection2_3.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void ReloadAutomatic()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);
            var store2 = new DataStore(newFilePath, reloadBeforeGetCollection: true);

            var collection1_1 = store.GetCollection("user");
            var collection2_1 = store2.GetCollection("user");
            Assert.Equal(3, collection1_1.Count);
            Assert.Equal(3, collection2_1.Count);

            collection1_1.InsertOne(new { id = 16, name = "Teddy" });
            Assert.Equal(4, collection1_1.Count);
            Assert.Equal(3, collection2_1.Count);

            var collection1_2 = store.GetCollection("user");
            var collection2_2 = store2.GetCollection("user");
            Assert.Equal(4, collection1_2.Count);
            Assert.Equal(4, collection2_2.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void FullTextSearch_Typed()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>();
            var matches = collection.Find("Box");
            Assert.Single(matches);

            var collection2 = store.GetCollection<Family>();

            var matches2 = collection2.Find("Hillsboro");
            Assert.Equal(5, matches2.Count());

            var matches21 = collection2.Find("hillsboro", true);
            Assert.Empty(matches21);

            var matches3 = collection2.Find("44").ToList();
            Assert.Equal(9, matches3.Count());

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void FullTextSearch_Dynamic()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            var matches = collection.Find("Box");
            Assert.Single(matches);

            var collection2 = store.GetCollection("family");

            var matches2 = collection2.Find("Hillsboro");
            Assert.Equal(5, matches2.Count());

            var matches21 = collection2.Find("hillsboro", true);
            Assert.Empty(matches21);

            var matches3 = collection2.Find("44");
            Assert.Equal(9, matches3.Count());

            UTHelpers.Down(newFilePath);
        }
    }
}