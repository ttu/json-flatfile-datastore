using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class CollectionTests
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

            collection.InsertOne(new { id = nextId + 1 });
            collection.InsertOne(new { id = nextId });

            nextId = collection.GetNextIdValue();
            Assert.Equal(6, nextId);
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
            Assert.Equal("hello5", nextId);

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
            collection.ReplaceOne(e => true, JToken.Parse("{ 'myId': 'test1' }"), true);
            collection.InsertOne(JToken.Parse("{ 'myId': 'test3' }"));
            collection.InsertOne(JToken.Parse("{ 'myId': 'test2' }"));

            var nextId = collection.GetNextIdValue();
            Assert.Equal("test4", nextId);

            var nextUpdate = JToken.Parse("{ 'myId': 'somethingWrong4' }");
            collection.InsertOne(nextUpdate);
            Assert.Equal(nextId, nextUpdate["myId"]);

            nextId = collection.GetNextIdValue();
            Assert.Equal("test5", nextId);

            nextUpdate = JToken.Parse("{ 'xxx': 111 }");
            collection.InsertOne(nextUpdate);
            Assert.Equal(nextId, nextUpdate["myId"]);
            Assert.Equal(111, nextUpdate["xxx"]);

            nextId = collection.GetNextIdValue();
            Assert.Equal("test6", nextId);
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
        public void InsertOne_DynamicUser()
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
        public void InsertOne_DynamicUser_UpdateId()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            Assert.Equal(3, collection.Count);

            collection.InsertOne(new { name = "Teddy" });
            Assert.Equal(4, collection.Count);

            var item = collection.AsQueryable().Single(e => e.name == "Teddy");

            Assert.Equal(4, item.id);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void InsertOne_TypedUser()
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
        public async Task InsertOneAsync_TypedUser()
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
        public async Task UpdateOne_TypedUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            var newUser = new User { Id = 11, Name = "Teddy", Age = 21 };
            var insertResult = collection.InsertOne(newUser);
            Assert.True(insertResult);
            Assert.Equal(4, collection.Count);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection<User>("user");
            Assert.Equal(4, collection2.Count);

            await collection2.UpdateOneAsync(e => e.Id == newUser.Id, new { Age = 22 });

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<User>("user");
            var updated = collection3.Find(e => e.Id == newUser.Id).First();
            Assert.Equal(22, updated.Age);
            Assert.Equal("Teddy", updated.Name);

            // Try to update property that doesn't exist
            collection2.UpdateOne(e => e.Id == 11, new { SomeThatIsNotThere = "No" });

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateOne_DynamicUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            Assert.Equal(3, collection.Count);

            await collection.InsertOneAsync(new { id = 11, name = "Teddy", age = 21 });

            dynamic source = new ExpandoObject();
            source.age = 22;
            var updateResult = await collection.UpdateOneAsync(e => e.id == 11, source as object);
            Assert.True(updateResult);

            await collection.UpdateOneAsync(e => e.id == 11, new { someThatIsNotThere = "No" });

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection("user");
            var updated = collection2.Find(e => e.id == 11).First();
            Assert.Equal(22, updated.age);
            Assert.Equal("Teddy", updated.name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateMany_DynamicUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            Assert.Equal(3, collection.Count);

            var newUsers = new[] {
                new { id = 20, name = "A1", age = 55 },
                new { id = 21, name = "A2", age = 55 },
                new { id = 22, name = "A3", age = 55 }
            };

            await collection.InsertManyAsync(newUsers);

            dynamic source = new ExpandoObject();
            source.age = 98;
            var updateResult = await collection.UpdateManyAsync(e => e.age == 55, source as object);
            Assert.True(updateResult);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection("user");
            var updated = collection2.Find(e => e.age == 98);
            Assert.Equal(3, updated.Count());

            await collection2.DeleteManyAsync(e => e.age == 98);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection("user");
            var updated2 = collection3.Find(e => e.age == 98);
            Assert.Empty(updated2);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateMany_JsonUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            Assert.Equal(3, collection.Count);

            var newUsersJson = @"
            [
                { 'id': 20, 'name': 'A1', 'age': 55 },
                { 'id': 21, 'name': 'A2', 'age': 55 },
                { 'id': 22, 'name': 'A3', 'age': 55 }
            ]
            ";

            var newUsers = JToken.Parse(newUsersJson);

            await collection.InsertManyAsync(newUsers);

            var newUserJson = "{ 'id': 23, 'name': 'A4', 'age': 22 }";
            var newUser = JToken.Parse(newUserJson);

            await collection.InsertOneAsync(newUser);

            dynamic source = new ExpandoObject();
            source.age = 98;
            var updateResult = await collection.UpdateManyAsync(e => e.age == 55, source as object);
            Assert.True(updateResult);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection("user");
            Assert.Equal(7, collection2.Count);
            var updated = collection2.Find(e => e.age == 98);
            Assert.Equal(3, updated.Count());

            await collection2.DeleteManyAsync(e => e.age == 98);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection("user");
            var updated2 = collection3.Find(e => e.age == 98);
            Assert.Empty(updated2);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void UpdateMany_TypedUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>();
            Assert.Equal(3, collection.Count);

            var newUsers = new[]
            {
                new User { Id = 20, Name = "A1", Age = 55 },
                new User { Id = 21, Name = "A2", Age = 55 },
                new User { Id = 22, Name = "A3", Age = 55 }
            };

            collection.InsertMany(newUsers);

            dynamic source = new ExpandoObject();
            source.Age = 98;
            var updateResult = collection.UpdateMany(e => e.Age == 55, source as object);
            Assert.True(updateResult);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection<User>();
            var updated = collection2.Find(e => e.Age == 98);
            Assert.Equal(3, updated.Count());

            collection2.DeleteMany(e => e.Age == 98);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<User>();
            var updated2 = collection3.Find(e => e.Age == 98);
            Assert.Empty(updated2);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void ReplaceOne_TypedUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            var newUser = new User { Id = 11, Name = "Teddy" };
            collection.InsertOne(newUser);
            Assert.Equal(4, collection.Count);

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection<User>("user");
            Assert.Equal(4, collection2.Count);

            collection2.ReplaceOne(e => e.Id == newUser.Id, new User { Id = newUser.Id, Name = "Theodor" });

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<User>("user");
            var updated = collection3.Find(e => e.Id == newUser.Id).First();
            Assert.Equal("Theodor", updated.Name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void ReplaceOne_Upsert_TypedUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            var success = collection.ReplaceOne(e => e.Id == 11, new User { Id = 11, Name = "Theodor" });
            Assert.False(success);

            success = collection.ReplaceOne(e => e.Id == 11, new User { Id = 11, Name = "Theodor" }, true);
            Assert.True(success);
            Assert.Equal(4, collection.Count);

            success = collection.ReplaceOne(e => e.Id == 11, new User { Id = 11, Name = "Jimmy" }, true);
            Assert.True(success);
            Assert.Equal(4, collection.Count);

            var fromDb = collection.AsQueryable().SingleOrDefault(e => e.Id == 11);
            Assert.NotNull(fromDb);
            Assert.Equal("Jimmy", fromDb.Name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void ReplaceOne_Upsert_DynamicUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            Assert.Equal(3, collection.Count);

            var success = collection.ReplaceOne(e => e.id == 11, new { id = 11, name = "Theodor" });
            Assert.False(success);

            success = collection.ReplaceOne(e => e.id == 11, new { id = 11, name = "Theodor" }, true);
            Assert.True(success);
            Assert.Equal(4, collection.Count);

            success = collection.ReplaceOne(e => e.id == 11, new { id = 11, name = "Jimmy" }, true);
            Assert.True(success);
            Assert.Equal(4, collection.Count);

            var fromDb = collection.AsQueryable().SingleOrDefault(e => e.id == 11);
            Assert.NotNull(fromDb);
            Assert.Equal("Jimmy", fromDb.name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void ReplaceOne_Upsert_DynamicWithInnerData()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("sensor");

            var success = collection.ReplaceOne(e => e.id == 11, JToken.Parse("{ 'id': 11, 'mac': 'F4:A5:74:89:16:57', 'data': { 'temperature': 20.5 } }"), true);
            Assert.True(success);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void ReplaceMany_TypedUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            var newUser1 = new User { Id = 11, Name = "Teddy" };
            var newUser2 = new User { Id = 11, Name = "Teddy2" };
            collection.InsertOne(newUser1);
            collection.InsertOne(newUser2);
            Assert.Equal(5, collection.Count);

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection<User>("user");
            Assert.Equal(5, collection2.Count);

            collection2.ReplaceMany(e => e.Name.Contains("Teddy"), new User { Id = 11, Name = "Theodor" });

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<User>("user");
            var updated = collection3.Find(e => e.Id == 11 && e.Name == "Theodor");
            Assert.Equal(2, updated.Count());

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void ReplaceOne_DynamicUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            Assert.Equal(3, collection.Count);

            dynamic dT = new { id = 11, name = "Teddy" };
            collection.InsertOne(dT);
            dynamic dC = new { id = 12, name = "Charlie" };
            collection.InsertOne(dC);
            Assert.Equal(5, collection.Count);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection("user");
            Assert.Equal(5, collection2.Count);

            dynamic d2 = new { id = 11, name = "Theodor" };
            collection2.ReplaceOne(e => e.id == 11, d2 as object);

            dynamic d3 = new { id = 12, name = "Charlton" };
            collection2.ReplaceOne((Predicate<dynamic>)(e => e.id == 12), d3);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection("user");

            var updated = collection3.Find(e => e.id == 11).First();
            Assert.Equal("Theodor", updated.name);

            var updated2 = collection3.Find(e => e.id == 12).First();
            Assert.Equal("Charlton", updated2.name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void DeleteOne_TypedUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            var newUser = new User { Id = 11, Name = "Teddy" };
            collection.InsertOne(newUser);
            Assert.Equal(4, collection.Count);

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection<User>("user");
            collection2.DeleteOne(e => e.Id == newUser.Id);
            Assert.Equal(3, collection2.Count);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<User>("user");
            Assert.Equal(3, collection3.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task DeleteOneAsync_DynamicNewCollection()
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

            var deleteResult = await collection2.DeleteOneAsync(e => e.Id == 1);
            Assert.True(deleteResult);
            Assert.Equal(0, collection2.Count);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection("book");
            Assert.Equal(0, collection3.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task DeleteMany_NotFoundAndFound()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath, false);
            var collection = store.GetCollection("user");

            var result = await collection.DeleteManyAsync(e => e.id == 56789);
            Assert.False(result);

            result = await collection.DeleteManyAsync(e => e.id == 1);
            Assert.True(result);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task InsertOne_100SyncThreaded()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collectionPreCheck = store.GetCollection<User>("user");
            Assert.Equal(3, collectionPreCheck.Count);

            var tasks = Enumerable.Range(0, 2).Select(cId =>
            {
                return Task.Run(() =>
                {
                    var collection = store.GetCollection<User>($"user_{cId}");

                    for (int i = 0; i < 50; i++)
                    {
                        collection.InsertOne(new User { Id = i, Name = $"Teddy_{i}" });
                    }

                    return true;
                });
            });

            await Task.WhenAll(tasks);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection<User>("user");

            Assert.Equal(3, collection2.Count);

            var collection_0 = store2.GetCollection<User>("user_0");
            Assert.Equal(50, collection_0.Count);

            var distinct_0a = collection_0.AsQueryable().GroupBy(e => e.Id).Select(g => g.First());
            Assert.Equal(50, distinct_0a.Count());

            var distinct_0b = collection_0.AsQueryable().GroupBy(e => e.Name).Select(g => g.First());
            Assert.Equal(50, distinct_0b.Count());

            var collection_1 = store2.GetCollection<User>("user_1");
            Assert.Equal(50, collection_1.Count);

            var distinct_1a = collection_1.AsQueryable().GroupBy(e => e.Id).Select(g => g.First());
            Assert.Equal(50, distinct_1a.Count());

            var distinct_1b = collection_1.AsQueryable().GroupBy(e => e.Name).Select(g => g.First());
            Assert.Equal(50, distinct_1b.Count());

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task InsertOne_100Async()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");
            Assert.Equal(3, collection.Count);

            var tasks = Enumerable.Range(0, 100)
                .AsParallel()
                .Select(i => collection.InsertOneAsync(new User { Id = i, Name = $"Teddy_{i}" }))
                .ToList();

            await Task.WhenAll(tasks);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection<User>("user");

            Assert.Equal(103, collection2.Count);

            var distinct = collection2.AsQueryable().GroupBy(e => e.Id).Select(g => g.First());
            Assert.Equal(103, distinct.Count());

            var distinct2 = collection2.AsQueryable().GroupBy(e => e.Name).Select(g => g.First());
            Assert.Equal(103, distinct2.Count());

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void UpdateOne_InnerExpandos()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user");

            var user = new User
            {
                Id = 4,
                Name = "Timmy",
                Age = 30,
                Work = new WorkPlace { Name = "EMACS" }
            };

            collection.InsertOne(user);

            var patchData = new Dictionary<string, object>
            {
                { "Age", 41 },
                { "Name", "James" },
                { "Work", new Dictionary<string, object> { { "Name", "ACME" } } }
            };
            var jobject = JObject.FromObject(patchData);
            dynamic patchExpando = JsonConvert.DeserializeObject<ExpandoObject>(jobject.ToString());

            collection.UpdateOne(i => i.Id == 4, patchExpando as object);

            var collection2 = store.GetCollection<User>("user");
            var userCheck = collection2.Find(i => i.Id == 4).FirstOrDefault();
            Assert.Equal("James", userCheck.Name);
            Assert.Equal("ACME", userCheck.Work.Name);

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
        public void ReloadAutiomatic()
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

        [Theory]
        [InlineData("datastore_dispose_false", false)]
        [InlineData("datastore_dispose_true", true)]
        public async Task DataStore_Dispose(string testName, bool useDispose)
        {
            // This test is extremely unreliable because of use GC, so if this test fails, run again

            // Fail the test is running time is more than maxTimeMs
            var sw = Stopwatch.StartNew();
            int maxTimeMs = 20000;

            var newFilePath = UTHelpers.Up(testName);

            int itemCount = 200;

            WeakReference storeRef = null;

            Task.Run(() => RunDataStore(out storeRef, newFilePath, itemCount, useDispose));

            var store = new DataStore(newFilePath, reloadBeforeGetCollection: true);

            while (true)
            {
                var collection = store.GetCollection("random");

                if (collection.Count == itemCount)
                    break;

                await Task.Delay(1000);

                if (sw.ElapsedMilliseconds > maxTimeMs)
                    Assert.False(true, "Timeout");
            }

            while (useDispose == storeRef.IsAlive)
            {
                await Task.Delay(1000);
                GC.Collect();

                if (sw.ElapsedMilliseconds > maxTimeMs)
                    Assert.False(true, "Timeout");
            }

            // If DataStore is not disposed, it should still be alive
            Assert.NotEqual(useDispose, storeRef.IsAlive);

            UTHelpers.Down(newFilePath);
        }

        private void RunDataStore(out WeakReference storeRef, string newFilePath, int count, bool dispose = false)
        {
            var store = new DataStore(newFilePath);

            storeRef = new WeakReference(store);

            var collection = store.GetCollection("random");

            var tasks = Enumerable.Range(0, count)
             .AsParallel()
             .Select(i => collection.InsertOneAsync(new User { Id = i, Name = $"Teddy_{i}" }))
             .ToList();

            if (dispose)
                store.Dispose();

            store = null;
        }
    }
}