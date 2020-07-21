using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class CollectionModificationTests
    {
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
        public void InsertOne_TypedUser_NewUser_WithId()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("user2");
            Assert.Equal(0, collection.Count);

            collection.InsertOne(new User { Id = 12, Name = "Teddy" });
            Assert.Equal(1, collection.Count);

            var collection2 = store.GetCollection<User>("user2");
            Assert.Equal(1, collection2.Count);

            var store2 = new DataStore(newFilePath);

            var collection3 = store2.GetCollection<User>("user2");
            Assert.Equal(1, collection3.Count);

            var item = collection3.AsQueryable().SingleOrDefault(e => e.Id == 12);
            Assert.NotNull(item);

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
        public async Task UpdateOneAsync_TypedUser_WrongCase()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<User>("users2");

            await collection.InsertOneAsync(new User { Id = 0, Name = "original" });
            await collection.InsertOneAsync(new User { Id = 1, Name = "original 2" });

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection<User>("users2");
            await collection2.UpdateOneAsync(x => x.Id == 0, new { name = "new value" });
            await collection2.UpdateOneAsync(x => x.Id == 1, JToken.Parse("{ name: \"new value 2\"} "));

            var store3 = new DataStore(newFilePath);

            var collection3 = store3.GetCollection<User>("users2");
            var items = collection3.AsQueryable();
            Assert.Equal(2, items.Count());

            Assert.Equal("new value", items.First().Name);
            Assert.Equal("new value 2", items.Last().Name);
        }

        [Fact]
        public async Task UpdateOneAsync_TypedUser()
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
            var notThereResult = await collection2.UpdateOneAsync(e => e.Id == newUser.Id, new { SomeThatIsNotThere = "No" });
            Assert.True(notThereResult);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateOneAsync_TypedModel_InnerSimpleArray()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<TestModelWithStringArray>();
            Assert.Equal(0, collection.Count);

            var newModel = new TestModelWithStringArray
            {
                Id = Guid.NewGuid().ToString(),
                Type = "empty",
                Fragments = new List<string>
                {
                    Guid.NewGuid().ToString()
                }
            };

            var insertResult = collection.InsertOne(newModel);
            Assert.True(insertResult);
            Assert.Equal(1, collection.Count);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection<TestModelWithStringArray>();
            Assert.Equal(1, collection2.Count);

            var updateData = new
            {
                Type = "filled",
                Fragments = new List<string>
                {
                     Guid.NewGuid().ToString(),
                     Guid.NewGuid().ToString()
                }
            };

            await collection2.UpdateOneAsync(e => e.Id == newModel.Id, updateData);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<TestModelWithStringArray>();
            var updated = collection3.Find(e => e.Id == newModel.Id).First();
            Assert.Equal(2, updated.Fragments.Count());
            Assert.Equal(2, updated.Fragments.Count());
            Assert.Equal(2, updated.Fragments.Count());
            Assert.Equal("filled", updated.Type);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateOneAsync_TypedModel_InnerSimpleIntArray()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<TestModelWithIntArray>();
            Assert.Equal(0, collection.Count);

            var newModel = new TestModelWithIntArray
            {
                Id = Guid.NewGuid().ToString(),
                Type = "empty",
                Fragments = new List<int>
                {
                    1
                }
            };

            var insertResult = collection.InsertOne(newModel);
            Assert.True(insertResult);
            Assert.Equal(1, collection.Count);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection<TestModelWithIntArray>();
            Assert.Equal(1, collection2.Count);

            var updateData = new
            {
                Type = "filled",
                Fragments = new List<int>
                {
                     2,
                     3
                }
            };

            await collection2.UpdateOneAsync(e => e.Id == newModel.Id, updateData);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection<TestModelWithIntArray>();
            var updated = collection3.Find(e => e.Id == newModel.Id).First();
            Assert.Equal(2, updated.Fragments.Count());
            Assert.Equal(2, updated.Fragments.First());
            Assert.Equal(3, updated.Fragments.Last());
            Assert.Equal("filled", updated.Type);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateOneAsync_Predicate_Id_DynamicUser()
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

            var resultNotThere = await collection.UpdateOneAsync(11, new { someThatIsNotThere = "No" });
            Assert.True(resultNotThere);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection("user");
            var updated = collection2.Find(e => e.id == 11).First();
            Assert.Equal(22, updated.age);
            Assert.Equal("Teddy", updated.name);
            Assert.Equal("No", updated.someThatIsNotThere);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateOneAsync_DynamicUser_WrongCase()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("user");
            Assert.Equal(3, collection.Count);

            await collection.InsertOneAsync(new { id = 11, name = "Teddy", age = 21 });

            dynamic source = new ExpandoObject();
            source.Age = 22;
            var updateResult = await collection.UpdateOneAsync(e => e.id == 11, source as object);
            Assert.True(updateResult);

            var store2 = new DataStore(newFilePath);
            var collection2 = store2.GetCollection("user");
            var updated = collection2.Find(e => e.id == 11).First();
            Assert.Equal(22, updated.age);
            Assert.Equal("Teddy", updated.name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateManyAsync_DynamicUser()
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
        public async Task UpdateManyAsync_JsonUser()
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
        public void ReplaceOne__Preditacte_Id_TypedUser()
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

            collection3.ReplaceOne(newUser.Id, new User { Id = newUser.Id, Name = "Theodor_2" });

            var store4 = new DataStore(newFilePath);
            var collection4 = store4.GetCollection<User>("user");
            var updated_2 = collection4.Find(e => e.Id == newUser.Id).First();
            Assert.Equal("Theodor_2", updated_2.Name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void ReplaceOne_Upsert__Predicate_Id_TypedUser()
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

            success = collection.ReplaceOne(11, new User { Id = 11, Name = "Jimmy" }, true);
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
        public void DeleteOne_With_Id_TypedUser()
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

            collection2.DeleteOne(newUser.Id);
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
        public async Task DeleteOneAsync_With_Id_DynamicNewCollection()
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

            var deleteResult = await collection2.DeleteOneAsync(1);
            Assert.True(deleteResult);
            Assert.Equal(0, collection2.Count);

            var store3 = new DataStore(newFilePath);
            var collection3 = store3.GetCollection("book");
            Assert.Equal(0, collection3.Count);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task DeleteManyAsync_NotFoundAndFound()
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
                { "name", "James" },
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
        public void UpdateOne_InnerCollection_FromAndToNull_Typed()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<Family>();

            var family = collection
                             .Find(p => p.Id == 4)
                             .First();

            Assert.Null(family.Children[1].Friends);

            var originalCount = family.Children[0].Friends.Count;

            family.Children[1].Friends = family.Children[0].Friends.ToList();
            family.Children[0].Friends = null;

            collection.UpdateOne(family.Id, family);

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection<Family>();

            var family_updated = collection2
                            .Find(p => p.Id == 4)
                            .First();

            Assert.Equal(originalCount, family_updated.Children[1].Friends.Count);
            Assert.Null(family_updated.Children[0].Friends);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void UpdateOne_InnerCollection_FromAndToNull_Dynamic()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection("family");

            var family = collection
                             .Find(p => p.id == 4)
                             .First();

            Assert.Null(family.children[1].friends);

            var origCount = family.children[0].friends.Count;

            family.children[1].friends = ((List<dynamic>)family.children[0].friends).ToList();
            family.children[0].friends = null;

            collection.UpdateOne(family.id, family);

            var store2 = new DataStore(newFilePath);

            var collection2 = store2.GetCollection<Family>();

            var family_updated = collection2
                            .Find(p => p.Id == 4)
                            .First();

            Assert.Equal(origCount, family_updated.Children[1].Friends.Count);
            Assert.Null(family_updated.Children[0].Friends);

            UTHelpers.Down(newFilePath);
        }
    }
}