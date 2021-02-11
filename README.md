JSON Flat File Data Store
----------------------------------

[![NuGet](https://img.shields.io/nuget/v/JsonFlatFileDataStore.svg)](https://www.nuget.org/packages/JsonFlatFileDataStore/)
[![NuGetCount](https://img.shields.io/nuget/dt/JsonFlatFileDataStore.svg
)](https://www.nuget.org/packages/JsonFlatFileDataStore/)

| Build server| Platform       | Build status      |
|-------------|----------------|-------------|
| AppVeyor    | Windows        |[![Build status](https://ci.appveyor.com/api/projects/status/adq9as6ruraln8tn?svg=true&branch=master)](https://ci.appveyor.com/project/ttu/json-flatfile-datastore)|
| Travis      | Linux / macOS  |[![Build Status](https://travis-ci.org/ttu/json-flatfile-datastore.svg?branch=master)](https://travis-ci.org/ttu/json-flatfile-datastore)|

Simple data store that saves the data in JSON format to a single file.

* Small API with basic functionality that is needed for handling data
* Works with dynamic and typed data
* Synchronous and asynchronous methods
* Data is stored in a JSON file
  * Easy to initialize
  * Easy to edit
  * Perfect for small apps and prototyping
* [.NET Standard 2.0](https://github.com/dotnet/standard/blob/master/docs/versions/netstandard2.0.md)
  * .NET Core 2.0 & .NET Framework 4.6.1

## Installation

You can install the latest version via [NuGet](https://www.nuget.org/packages/JsonFlatFileDataStore/).

```sh
# .NET Core CLI
$ dotnet add package JsonFlatFileDataStore

# Package Manager Console
PM> Install-Package JsonFlatFileDataStore
```

## Example project

[Fake JSON Server](https://github.com/ttu/dotnet-fake-json-server) is an ASP.NET Core Web App which uses JSON Flat File Data Store with dynamic data.

## Example

#### Typed data

```csharp
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

// Open database (create new if file doesn't exist)
var store = new DataStore("data.json");

// Get employee collection
var collection = store.GetCollection<Employee>();

// Create new employee instance
var employee = new Employee { Id = 1, Name = "John", Age = 46 };

// Insert new employee
// Id is updated automatically to correct next value
await collection.InsertOneAsync(employee);

// Update employee
employee.Name = "John Doe";

await collection.UpdateOneAsync(employee.Id, employee);

// Use LINQ to query items
var results = collection.AsQueryable().Where(e => e.Age > 30);

// Save instance as a single item
await store.InsertItemAsync("selected_employee", employee);

// Single items can be of any type
await store.InsertItemAsync("counter", 1);
var counter = await store.GetItem<int>("counter");
```

#### Dynamically typed data

Dynamic data can be `Anonymous type`, `ExpandoObject`, JSON objects (`JToken`, `JObject`, `JArray`) or `Dictionary<string, object>`. Internally dynamic data is serialized to `ExpandoObject`.

```csharp
// Open database (create new if file doesn't exist)
var store = new DataStore(pathToJson);

// Get employee collection
var collection = store.GetCollection("employee");

// Create new employee
var employee = new { id = 1, name = "John", age = 46 };

// Create new employee from JSON
var employeeJson = JToken.Parse("{ 'id': 2, 'name': 'Raymond', 'age': 32 }");

// Create new employee from dictionary
var employeeDict = new Dictionary<string, object>
{
    ["id"] = 3,
    ["name"] = "Andy",
    ["age"] = 32
};

// Insert new employee
// Id is updated automatically if object is updatable
await collection.InsertOneAsync(employee);
await collection.InsertOneAsync(employeeJson);
await collection.InsertOneAsync(employeeDict);

// Update data from anonymous type
var updateData = new { name = "John Doe" };

// Update data from JSON
var updateJson = JToken.Parse("{ 'name': 'Raymond Doe' }");

// Update data from dictionary
var updateDict = new Dictionary<string, object> { ["name"] = "Andy Doe" };

await collection.UpdateOneAsync(e => e.id == 1, updateData);
await collection.UpdateOneAsync(e => e.id == 2, updateJson);
await collection.UpdateOneAsync(e => e.id == 3, updateDict);

// Use LINQ to query items
var results = collection.AsQueryable().Where(x => x.age > 30);
```

## Functionality

### Collections

Example user collection in JSON:

```json
{
  "user": [
    { "id": 1, "name": "Phil", "age": 40, "city": "NY" },
    { "id": 2, "name": "Larry", "age": 37, "city": "London" }
  ]
}
```

#### Query

Collection can be queried with LINQ by getting queryable from the collection with `AsQueryable` method.

NOTE: `AsQueryable` returns `IEnumerable`, instead of `IQueryable`, because `IQueryable` doesn't support Dynamic types in LINQ queries. With this data store it won't matter as all data is already loaded into memory.

`AsQueryable` LINQ query with dynamic data:

```csharp
var store = new DataStore(pathToJson);

var collection = store.GetCollection("user");

// Find item with name
var userDynamic = collection
                    .AsQueryable()
                    .FirstOrDefault(p => p.name == "Phil");
```

`AsQueryable` LINQ query with typed data:

```csharp
var store = new DataStore(pathToJson);

var collection = store.GetCollection<User>();

// Find item with name
var userTyped = collection
                    .AsQueryable()
                    .FirstOrDefault(p => p.Name == "Phil");
```

#### Full-text search

Full-text search can be performed with `Find` method. Full-text search does deep search on all child objects. __By default__ the search is not case sensitive.

```csharp
var store = new DataStore(pathToJson);

var collection = store.GetCollection("user");

// Find all users that contain text Alabama in any of property values
var matches = collection.Find("Alabama");

// Perform case sensitive search
var caseSensitiveMatches = collection.Find("Alabama", true);

```

#### Insert

`InsertOne` and `InsertOneAsync` will insert a new item to the collection. Method returns true if insert was successful.

```csharp
// Asynchronous method and dynamic data
// Before update : { }
// After update  : { "id": 3, "name": "Raymond", "age": 32, "city" = "NY" }
await collection.InsertOneAsync(new { id = 3, name = "Raymond", age = 32, city = "NY" });

// Dynamic item can also be JSON object
var user = JToken.Parse("{ 'id': 3, 'name': 'Raymond', 'age': 32, 'city': 'NY' }");
await collection.InsertOneAsync(user);

// Synchronous method and typed data
// Before update : { }
// After update  : { "id": 3, "name": "Raymond", "age": 32, "city" = "NY" }
collection.InsertOne(new User { Id = 3, Name = "Raymond", Age = 32, City = "NY" });
```

`InsertMany` and `InsertManyAsync` will insert a list of items to the collection.
```csharp
var newItems = new[]
{
    new User { Id = 3, Name = "Raymond", Age = 32, City = "NY" },
    new User { Id = 4, Name = "Ted", Age = 43, City = "NY" }
};

collection.InsertMany(newItems);
```

`Insert`-methods will update the inserted object's `Id`-field, if it has a field with that name and the field is writable. If the `Id`-field is missing from the dynamic object, a field is added with the correct value. If an `Anonymous type` is used for insert, `id` will be added to the persisted object if the `id`-field is missing. If the `id` is present, then that value will be used.

```csharp
var newItems = new[]
{
    new { id = 14, name = "Raymond", age = 32, city = "NY" },
    new { id = 68, name = "Ted", age = 43, city = "NY" },
    new { name = "Bud", age = 43, city = "NY" }
};

// Last user will have id 69
collection.InsertMany(newItems);
// Item in newItems collection won't have id property as anonymous types are read only
```

If the `id`-field 's type is a *number*, value is incremented by one. If the type is a *string*, incremented value number is added to the end of the initial text.

```csharp
// Latest id in the collection is hello5
var user = JToken.Parse("{ 'id': 'wrongValue', 'name': 'Raymond', 'age': 32, 'city': 'NY' }");
await collection.InsertOneAsync(user);
// After addition: user["id"] == "hello6"

// User data doesn't have an id field
var userNoId = JToken.Parse("{ 'name': 'Raymond', 'age': 32, 'city': 'NY' }");
await collection.InsertOneAsync(userNoId);
// After addition: userNoId["id"] == "hello7"
```

If collection is empty and the type of the id-field is number, then first id will be `0`. If type is string then first id will be `"0"`. 

#### Replace

`ReplaceOne` and `ReplaceOneAsync` will replace the first item that matches the filter or provided id-value matches the defined id-field. Method will return true if item(s) found with the filter.

```csharp
// Sync and dynamic
// Before update : { "id": 3, "name": "Raymond", "age": 32, "city": "NY" }
// After update  : { "id": 3, "name": "Barry", "age": 42 }
collection.ReplaceOne(3, new { id = 3, name = "Barry", age = 33 });
// or with predicate
collection.ReplaceOne(e => e.id == 3, new { id = 3, name = "Barry", age = 33 });

// Async and typed
// Before update : { "id": 3, "name": "Raymond", "age": 32, "city": "NY" }
// After update  : { "id": 3, "name": "Barry", "age": 42 }
await collection.ReplaceOneAsync(3, new User { Id = 3, Name = "Barry", Age = 33 });
```

`ReplaceMany` and `ReplaceManyAsync` will replace all items that match the filter.

```csharp
collection.ReplaceMany(e => e.City == "NY", new { City = "New York" });
```

`ReplaceOne` and `ReplaceOneAsync` have an upsert option. If the item to replace doesn't exists in the data store, new item will be inserted. Upsert won't update id, so new item will be inserted with the id that it has.

```csharp
// New item will be inserted with id 11
collection.ReplaceOne(11, new { id = 11, name = "Theodor" }, true);
```

#### Update

`UpdateOne` and `UpdateOneAsync` will update the first item that matches the filter or provided id-value matches the defined id-field. Properties to update are defined with dynamic object. Dynamic object can be an `Anonymous type` or an `ExpandoObject`. Method will return true if item(s) found with the filter.

```csharp
// Dynamic
// Before update : { "id": 1, "name": "Barry", "age": 33 }
// After update  : { "id": 1, "name": "Barry", "age": 42 }
dynamic source = new ExpandoObject();
source.age = 42;
await collection.UpdateOneAsync(1, source as object);
// or with predicate
await collection.UpdateOneAsync(e => e.id == 1, source as object);

// Typed
// Before update : { "id": 1, "name": "Phil", "age": 40, "city": "NY" }
// After update  : { "id": 1, "name": "Phil", "age": 42, "city": "NY" }
await collection.UpdateOneAsync(e => e.Name == "Phil", new { age = 42 });
```

`UpdateMany` and `UpdateManyAsync` will update all items that match the filter.

```csharp
await collection.UpdateManyAsync(e => e.Age == 30, new { age = 31 });
```

Update can also update items from the collection and add new items to the collection. `null` items in the passed update data are skipped, so with `null` items data in the correct index can be updated.

```csharp
var family = new Family
{
    Id = 12,
    FamilyName = "Andersen",
    Parents = new List<Parent>
    {
        new Parent {  FirstName = "Jim", Age = 52 }
    },
    Address = new Address { City = "Helsinki" }
};

await collection.InsertOneAsync(family);

// Adds a second parent to the list
await collection.UpdateOneAsync(e => e.Id == 12, new { Parents = new[] { null, new { FirstName = "Sally", age = 41 } } });

// Updates the first parent's age to 42
await collection.UpdateOneAsync(e => e.Id == 12, new { Parents = new[] { new { age = 42 } } });
```

Easy way to create a patch `ExpandoObject` on runtime is to create a `Dictionary` and then to serialize it to a JSON and deserialize to an `ExpandoObject`.

```csharp
var user = new User
{
    Id = 12,
    Name = "Timmy",
    Age = 30,
    Work = new WorkPlace { Name = "EMACS" }
};

// JSON: { "Age": 41, "Name": "James", "Work": { "Name": "ACME" } }
// Anonymous type: new { Age = 41, Name = "James", Work = new { Name = "ACME" } };
var patchData = new Dictionary<string, object>();
patchData.Add("Age", 41);
patchData.Add("Name", "James");
patchData.Add("Work", new Dictionary<string, object> { { "Name", "ACME" } });

var jobject = JObject.FromObject(patchData);
dynamic patchExpando = JsonConvert.DeserializeObject<ExpandoObject>(jobject.ToString());

await collection.UpdateOneAsync(e => e.Id == 12, patchExpando);
```

##### Limitations

Dictionaries won't work when serializing JSON or data to `ExpandoObjects`. This is becauses dictionaries and objects are similar when serialized to JSON, so serialization creates an `ExpandoObject` from `Dictionary`. Update's are mainly meant to be used with `HTTP PATCH`, so normally `Replace` is easier and better way to update data.

If the update `ExpandoObject` is created manually, then the Dictionaries content can be updated. Unlike `List`, `Dictionary`'s whole content is replaced with the update data's content.

```csharp
var player = new Player
{
    Id = 423,
    Scores = new Dictionary<string, int>
    {
        { "Blue Max", 1256 },
        { "Pacman", 3221 }
    },
};

var patchData = new ExpandoObject();
var items = patchData as IDictionary<string, object>;
items.Add("Scores", new Dictionary<string, string> { { "Blue Max", 1345 }, { "Outrun", 1234 }, { "Pacman", 3221 }, });

await collection.UpdateOneAsync(e => e.Id == 423, patchData);
```

#### Delete

`DeleteOne` and `DeleteOneAsync` will remove the first object that matches the filter or provided id-value matches the defined id-field. Method returns true if item(s) found with the filter or with the id.

```csharp
// Dynamic
await collection.DeleteOneAsync(3);
await collection.DeleteOneAsync(e => e.id == 3);

// Typed
await collection.DeleteOneAsync(3);
await collection.DeleteOneAsync(e => e.Id == 3);
```

`DeleteMany` and `DeleteManyAsync` will delete all items that match the filter. Method returns true if item(s) found with the filter.

```csharp
// Dynamic
await collection.DeleteManyAsync(e => e.city == "NY");

// Typed
await collection.DeleteManyAsync(e => e.City == "NY");
```

#### Id-field value


If incrementing Id-field values is used, `GetNextIdValue` returns next Id-field value. If Id-property is integer, last item's value is incremented by one. If field is not an integer, it is converted to a string and number is parsed from the end of the string and incremented by one.

```csharp
var store = new DataStore(newFilePath, keyProperty: "myId");

// myId is an integer
collection.InsertOne(new { myId = 2 });
// nextId = 3
var nextId = collection.GetNextIdValue();

// myId is a string
collection.InsertOne(new { myId = "hello" });
// nextId = "hello0"
var nextId = collection.GetNextIdValue();

collection.InsertOne(new { myId = "hello3" });
// nextId = "hello4"
var nextId = collection.GetNextIdValue();
```

### Single item

```json
{
  "selected_user": { "id": 1, "name": "Phil", "age": 40, "city": "NY" },
  "temperature": 23.45,
  "temperatues": [ 12.4, 12.42, 12.38 ],
  "note": "this is a test"
}
```

Data store supports single items. Items can be value and reference types. Single item supports dynamic and typed data.

Arrays are concidered as single items if they contain value types. If Array is empty it is listed as a collection.

Single item's support same methods as Collections (`Get`, `Insert`, `Replace`, `Update`, `Delete`).

#### Get

```csharp
var store = new DataStore(pathToJson);
// Typed data
var counter = store.GetItem<int>("counter");
// Dynamic data
var user = store.GetItem("myUser");
```

Typed data will throw `KeyNotFoundException` if key is not found. Dynamic data and nullable types will return null.

```csharp
// throw KeyNotFoundException
var counter = store.GetItem<int>("counter_NotFound");
var user = store.GetItem<User>("user_NotFound");
// return null
var counter = store.GetItem<int?>("counter_NotFound");
var counter = store.GetItem("counter_NotFound");
```

#### Insert

`InsertItem` and `InsertItemAsync` will insert a new item to the JSON. Method returns true if insert was successful.

```csharp
// Value type
var result = await store.InsertItemAsync("counter", 2);
// Reference type
var user = new User { Id = 12, Name = "Teddy" }
var result = await store.InsertItemAsync<User>("myUser", user);
```


#### Replace

`ReplaceItem` and `ReplaceItemAsync` will replace the item with the key. Method will return true if item is found with the key.

```csharp
// Value type
var result = await store.ReplaceItemAsync("counter", 4);
// Reference type
var result = await store.ReplaceItemAsync("myUser", new User { Id = 2, Name = "James" });
```

`ReplaceSingleItem` and `ReplaceSingleItem` have an upsert option. If the item to replace doesn't exists in the data store, new item will be inserted.
```csharp
// Value type
var result = await store.ReplaceItemAsync("counter", 4, true);
// Reference type
var result = await store.ReplaceItemAsync("myUser", new User { Id = 2, Name = "James" }, true);
```

#### Update

`UpdateItem` and `UpdateItemAsync` will update the first item that matches the filter with passed properties from dynamic object. Dynamic object can be an `Anonymous type` or an `ExpandoObject`. Method will return true if item is found with the key.

```csharp
// Value type
var result = await store.UpdateItemAsync("counter", 2);
// Reference type
var result = await store.UpdateItemAsync("myUser", new { name = "Harold" });
```

#### Delete

`DeleteItem` and `DeleteItemAsync` will remove the item that matches the key. Method returns true if item is found and deleted with the key.

```csharp
// Sync
var result = store.DeleteItem("counter");
// Async
var result = await store.DeleteItemAsync("counter");
```

## DataStore and Collection lifecycle

When the data store is created, it reads the JSON file to the memory. Data store starts a new background thread that handles the file access.

When the collection is created it has a lazy reference to the data and it will deserialize the JSON to objects when it is accessed for the first time.

All write operations in collections are executed immediately internally in the collection and then the same operation is queued on DataStore's BlockingCollection. Operations from the BlockingCollection are executed on background thread to DataStore's internal collection and saved to file.

```csharp
// Data is loaded from the file
var store = new DataStore(newFilePath);

// Lazy reference to the data is created
var collection1st = store.GetCollection("hello");
var collection2nd = store.GetCollection("hello");

// Data is loaded from the store to the collection and new item is inserted
collection1st.InsertOne(new { id = "hello" });

// Data is loaded from the store to the collection and new item is inserted
// This collection will also have item with id: hello as data is serialized when it is used for the first time
collection2nd.InsertOne(new { id = "hello2" });

// collection1st won't have item with id hello2
```

If multiple DataStores are initialized and used simultaneously, each DataStore will have its own internal state. They might become out of sync with the state in the JSON file, as data is only loaded from the file when DataStore is initialized and after each commit.

It is also possible to reload JSON data manually, by using DataStore's `Reload` method or set `reloadBeforeGetCollection` constructor parameter to `true`.

```csharp
// Data is loaded from the file
var store = new DataStore(newFilePath);
var store2 = new DataStore(newFilePath, reloadBeforeGetCollection: true);

var collection1_1 = store.GetCollection("hello");
collection1_1.InsertOne(new { id = "hello" });

// Because of reload collection2_1 will also have item with id: hello
var collection2_1 = store2.GetCollection("hello");

collection2_1.InsertOne(new { id = "hello2" });

store.Reload()

// Because of reload collection1_2 will also have item with id: hello2
var collection1_2 = store.GetCollection("hello");

// collection1_1 will not have item with id: hello2 even after reload, because it was initialized before reload
```

If JSON Flat File Data Store is used with e.g. `ASP.NET`, add the `DataStore` to the DI container as a singleton. This way DataStore's internal state is correct and application does not have to rely on the state on the file as read operation is pretty slow. Reload can be triggered if needed.

#### Dispose

Data store should be disposed after it is not needed anymore. Dispose will wait that all writes to the file are completed and after that it will stop the background thread. Then Garabge Collector can collect the data store that is not used anymore.

```csharp
// Call dispose method
var store = new DataStore();
// ...
store.Dispose();

// Call dispose automatically with using
using(var store = new DataStore())
{
    // ...
}
```

## Collection naming

Collection name must be always defined when dynamic collections are used. If collection name is not defined with a typed collection, class-name is converted to lower camel case. E.g. User is user, UserFamily is userfamily etc.

```csharp
var store = new DataStore(newFilePath);
// JSON { "movie": [] };
var collection = store.GetCollection("movie");
// JSON { "movie": [] };
var collection = store.GetCollection<Movie>();
// JSON { "movies": [] };
var collection = store.GetCollection<Movie>("movies");
```

## Writing to file

By default JSON is written in lower camel case. This can be changed with `useLowerCamelCase` parameter in DataStore's constructor.

```csharp
// This will write JSON in lower camel case
// e.g. { "myMovies" : [ { "longName": "xxxxx" } ] }
var store = new DataStore(newFilePath);

// This will write JSON in upper camel case
// e.g. { "MyMovies" : [ { "LongName": "xxxxx" } ] }
var store = new DataStore(newFilePath, false);
```

## Dynamic and error CS1977

When __Dynamic type__ is used with lambdas, compiler will give you error __CS1977__:

> CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type

A lambda needs to know the data type of the parameter at compile time. Cast dynamic to an object and compiler will happily accept it, as it believes you know what you are doing and leaves validation to Dynamic Language Runtime.

```csharp
dynamic dynamicUser = new { id = 11, name = "Theodor" };

// This will give CS1977 error
collection2.ReplaceOne(e => e.id == 11, dynamicUser);

// Compiler will accept this
collection2.ReplaceOne(e => e.id == 11, dynamicUser as object);

// Compiler will also accept this
collection2.ReplaceOne((Predicate<dynamic>)(e => e.id == 11), dynamicUser);
```

## Unit Tests & Benchmarks

`JsonFlatFileDataStore.Test` and `JsonFlatFileDataStore.Benchmark` are _.NET 5_ projects.

Unit Tests are executed automatically with CI builds.

Benchmarks are not part of CI builds. Benchmarks can be used as a reference when making changes to the existing functionality by comparing the execution times before and after the changes.

Run benchmarks from command line:
```sh
$ dotnet run --configuration Release --project JsonFlatFileDataStore.Benchmark\JsonFlatFileDataStore.Benchmark.csproj
```

## API

API is heavily influenced by MongoDB's C# API, so switching to the MongoDB or [DocumentDB](https://docs.microsoft.com/en-us/azure/documentdb/documentdb-protocol-mongodb) might be easy.
* [MongoDB-C#-linq](http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/linq/#queryable)
* [MongoDB-C#-crud](http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/writing/)

## Changelog

[Changelog](CHANGELOG.md)

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License

Licensed under the [MIT](LICENSE) License.
