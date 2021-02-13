using Newtonsoft.Json.Linq;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class DataStoreTests
    {
        [Fact]
        public void UpdateAll()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            store.UpdateAll("{ 'tasks': [ { 'id': 0, 'task': 'Commit'} ] }");

            var collection = store.GetCollection("tasks");
            Assert.Equal(1, collection.Count);

            var item = collection.AsQueryable().First();
            Assert.Equal("Commit", item.task);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void ListKeys()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collections = store.GetKeys();
            Assert.Equal("user", collections.First().Key);
            Assert.Equal(ValueType.Collection, collections.First().Value);
            Assert.Equal(ValueType.Item, collections.Last().Value);
            Assert.Equal(9, collections.Count());

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void ListCollections()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collections = store.GetKeys(ValueType.Collection);
            Assert.Equal("user", collections.First().Key);
            Assert.Equal(ValueType.Collection, collections.First().Value);
            Assert.Equal(4, collections.Count());

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void ListItems()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var items = store.GetKeys(ValueType.Item);
            Assert.Equal("myValue", items.First().Key);
            Assert.Equal(ValueType.Item, items.First().Value);
            Assert.Equal(5, items.Count());

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

            Assert.Contains("privateOwner", json);
            Assert.Contains("ownerLongTestProperty", json);

            var store2 = new DataStore(newFilePath);

            var collectionUppercase = store2.GetCollection<PrivateOwner>("PrivateOwner");
            Assert.Equal(0, collectionUppercase.Count);

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
        public void GetCollection_TypedCollection_NameParameter()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var collection = store.GetCollection<Movie>("movies");
            Assert.Equal(2, collection.Count);
            Assert.Single(collection.AsQueryable().Where(e => e.Name == "Predator"));

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void GetCollection_Mocked()
        {
            var innerCollection = new DocumentCollection<Movie>(
                new Func<string, Func<List<Movie>, bool>, bool, Task<bool>>((s, d, i) => Task.FromResult(true)),
                new Lazy<List<Movie>>(() => new List<Movie> { new Movie { Name = "Commando" } }),
                @"/path/to/file",
                "id",
                new Func<Movie, Movie>(m => m),
                new Func<Movie>(() => new Movie()));

            var store = Substitute.For<IDataStore>();
            store.GetCollection<Movie>().Returns(innerCollection);

            var collection = store.GetCollection<Movie>();
            Assert.Equal(1, collection.Count);

            var result = collection.InsertOne(new Movie { Name = "Predator" });
            Assert.True(result);
            Assert.Equal(2, collection.Count);
            Assert.Single(collection.AsQueryable().Where(e => e.Name == "Predator"));
        }

        [Fact]
        public void GetCollection_Exception()
        {
            var innerCollection = new DocumentCollection<Movie>(
                new Func<string, Func<List<Movie>, bool>, bool, Task<bool>>((s, d, i) => throw new Exception("Failed")),
                new Lazy<List<Movie>>(() => new List<Movie> { new Movie { Name = "Commando" } }),
                @"/path/to/file",
                "id",
                new Func<Movie, Movie>(m => m),
                new Func<Movie>(() => new Movie()));

            var store = Substitute.For<IDataStore>();
            store.GetCollection<Movie>().Returns(innerCollection);

            var collection = store.GetCollection<Movie>();

            Assert.Throws<Exception>(() => collection.InsertOne(new Movie { Name = "Predator" }));
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

        [Fact]
        public async Task Readme_Example2()
        {
            var pathToJson = UTHelpers.Up();

            // Open database (create new if file doesn't exist)
            var store = new DataStore(pathToJson);

            // Get employee collection
            var collection = store.GetCollection("employee");

            // Create new employee instance
            var employee = new
            {
                id = collection.GetNextIdValue(),
                name = "John",
                age = 46
            };

            // Example with JSON object
            var employeeJson = JToken.Parse("{ 'id': 2, 'name': 'Raymond', 'age': 32 }");

            // Example with JSON object
            var employeeDict = new Dictionary<string, object>
            {
                ["id"] = 3,
                ["name"] = "Andy",
                ["age"] = 32
            };

            var employeeExpando = new ExpandoObject();
            var items = employeeExpando as IDictionary<string, object>;
            items.Add("id", 3);
            items.Add("name", "Karl");
            items.Add("age", 29);

            // Insert new employee
            await collection.InsertOneAsync(employee);
            await collection.InsertOneAsync(employeeJson);
            await collection.InsertOneAsync(employeeDict);
            await collection.InsertOneAsync(employeeExpando);

            // As anonymous types property is read only we can use new anonymous type to update data
            var updateData = new { name = "John Doe" };
            await collection.UpdateOneAsync(e => e.id == employee.id, updateData);

            var updateJson = JObject.Parse("{ 'name': 'Raymond Doe' }");
            await collection.UpdateOneAsync(e => e.id == 1, updateJson);

            var updateDict = new Dictionary<string, object> { ["name"] = "Andy Doe" };
            await collection.UpdateOneAsync(e => e.id == 2, updateDict);

            // Use LINQ to query items
            var results = collection.AsQueryable().Where(x => x.age < 40);

            Assert.True(results.Count() == 3);
            Assert.Single(results.Where(e => e.name == "Raymond Doe"));
            Assert.Single(results.Where(e => e.name == "Andy Doe"));
            Assert.Single(results.Where(e => e.name == "Karl"));

            UTHelpers.Down(pathToJson);
        }

        [Fact]
        public async Task Insert_CorrectIdWithDynamic()
        {
            var pathToJson = UTHelpers.Up();

            var store = new DataStore(pathToJson);

            var collection = store.GetCollection("employee");

            // Create new employee instance
            var employee = new
            {
                id = 20,
                name = "John",
                age = 46
            };

            // Example with JSON object
            var employeeJson = JToken.Parse("{ 'id': 200, 'name': 'Raymond', 'age': 32 }");

            // Example with JSON object
            var employeeDict = new Dictionary<string, object>
            {
                ["id"] = 300,
                ["name"] = "Andy",
                ["age"] = 32
            };

            var employeeExpando = new ExpandoObject();
            var items = employeeExpando as IDictionary<string, object>;
            items.Add("id", 4000);
            items.Add("name", "Karl");
            items.Add("age", 34);

            // Insert new employee
            await collection.InsertOneAsync(employee);
            await collection.InsertOneAsync(employeeJson);
            await collection.InsertOneAsync(employeeDict);
            await collection.InsertOneAsync(employeeExpando);

            Assert.Equal(20, employee.id);
            Assert.Equal(21, employeeJson["id"]);
            Assert.Equal(22, employeeDict["id"]);
            Assert.Equal(23, ((IDictionary<string, object>)employeeExpando)["id"]);

            UTHelpers.Down(pathToJson);
        }

        [Fact]
        public async Task Insert_CorrectIdWithDynamic_No_InitialId()
        {
            var pathToJson = UTHelpers.Up();

            var store = new DataStore(pathToJson, keyProperty: "acc");

            var collection = store.GetCollection("employee");

            // Create new employee instance
            var employee = new
            {
                name = "John",
                age = 46
            };

            // Example with JSON object
            var employeeJson = JToken.Parse("{ 'name': 'Raymond', 'age': 32 }");

            // Example with JSON object
            var employeeDict = new Dictionary<string, object>
            {
                ["name"] = "Andy",
                ["age"] = 32
            };

            var employeeExpando = new ExpandoObject();
            var items = employeeExpando as IDictionary<string, object>;
            items.Add("name", "Karl");
            items.Add("age", 34);

            // Insert new employee
            await collection.InsertOneAsync(employee);
            await collection.InsertOneAsync(employeeJson);
            await collection.InsertOneAsync(employeeDict);
            await collection.InsertOneAsync(employeeExpando);

            Assert.Equal(1, employeeJson["acc"]);
            Assert.Equal(2, employeeDict["acc"]);
            Assert.Equal(3, ((IDictionary<string, object>)employeeExpando)["acc"]);

            UTHelpers.Down(pathToJson);
        }

        [Fact]
        public async Task Insert_CorrectIdWithDynamic_With_InitialId()
        {
            var pathToJson = UTHelpers.Up();

            var store = new DataStore(pathToJson, keyProperty: "acc");

            var collection = store.GetCollection("employee");

            // Create new employee instance
            var employee = new
            {
                acc = "hello",
                name = "John",
                age = 46
            };

            // Example with JSON object
            var employeeJson = JToken.Parse("{ 'name': 'Raymond', 'age': 32 }");

            // Example with JSON object
            var employeeDict = new Dictionary<string, object>
            {
                ["name"] = "Andy",
                ["age"] = 32
            };

            var employeeExpando = new ExpandoObject();
            var items = employeeExpando as IDictionary<string, object>;
            items.Add("name", "Karl");
            items.Add("age", 34);

            // Insert new employee
            await collection.InsertOneAsync(employee);
            await collection.InsertOneAsync(employeeJson);
            await collection.InsertOneAsync(employeeDict);
            await collection.InsertOneAsync(employeeExpando);

            Assert.Equal("hello", employee.acc);
            Assert.Equal("hello0", employeeJson["acc"]);
            Assert.Equal("hello1", employeeDict["acc"]);
            Assert.Equal("hello2", ((IDictionary<string, object>)employeeExpando)["acc"]);

            UTHelpers.Down(pathToJson);
        }

        [Fact]
        public async Task Readme_Example3()
        {
            var pathToJson = UTHelpers.Up();

            // Open database (create new if file doesn't exist)
            var store = new DataStore(pathToJson);

            // Get employee collection
            var collection = store.GetCollection<Employee>();

            // Create new employee instance
            var employee = new Employee
            {
                Name = "John",
                Age = 46
            };

            // Get next id value
            employee.Id = collection.GetNextIdValue();

            // Insert new employee
            await collection.InsertOneAsync(employee);

            // Update user
            employee.Name = "John Doe";

            await collection.UpdateOneAsync(e => e.Id == employee.Id, employee);

            // Use LINQ to query items
            var results = collection.AsQueryable().Where(e => e.Age > 30);

            Assert.Single(results.Where(e => e.Name == "John Doe"));

            UTHelpers.Down(pathToJson);
        }

        public class Employee
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}