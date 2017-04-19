using Newtonsoft.Json.Linq;
using NSubstitute;
using System;
using System.Collections.Generic;
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
        public void GetCollection_Mocked()
        {
            var innerCollection = new DocumentCollection<Movie>(
                new Func<string, Func<List<Movie>, bool>, bool, Task<bool>>((s, d, i) => { return Task.FromResult(true); }),
                new Lazy<List<Movie>>(() => new List<Movie> { new Movie { Name = "Commando" } }),
                @"/path/to/file",
                "id",
                new Func<Movie, Movie>(m => m));

            var store = Substitute.For<IDataStore>();
            store.GetCollection<Movie>().Returns(innerCollection);

            var collection = store.GetCollection<Movie>();
            Assert.Equal(1, collection.Count);

            var result = collection.InsertOne(new Movie { Name = "Predator" });
            Assert.True(result);
            Assert.Equal(2, collection.Count);
            Assert.Equal(1, collection.AsQueryable().Count(e => e.Name == "Predator"));
        }

        [Fact]
        public void GetCollection_Exception()
        {
            var innerCollection = new DocumentCollection<Movie>(
                new Func<string, Func<List<Movie>, bool>, bool, Task<bool>>((s, d, i) => { throw new Exception("Failed"); }),
                new Lazy<List<Movie>>(() => new List<Movie> { new Movie { Name = "Commando" } }),
                @"/path/to/file",
                "id",
                new Func<Movie, Movie>(m => m));

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

            // Get customer collection
            var collection = store.GetCollection("employee");

            // Create new user instance
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

            // Insert new user
            await collection.InsertOneAsync(employee);
            await collection.InsertOneAsync(employeeJson);
            await collection.InsertOneAsync(employeeDict);

            // As anonymous types property is read only we can use new anonymous type to update data
            var updateData = new { name = "John Doe" };

            await collection.UpdateOneAsync(e => e.id == employee.id, updateData);

            // Use LINQ to query items
            var results = collection.AsQueryable().Where(x => x.age > 30);

            Assert.True(results.Count() == 3);
            Assert.True(results.Count(e => e.name == "John Doe") == 1);

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

            Assert.True(results.Count(e => e.Name == "John Doe") == 1);

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