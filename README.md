JSON Flat File Datastore
----------------------------------

[![Build Status](https://travis-ci.org/ttu/json-flatfile-datastore.svg?branch=master)](https://travis-ci.org/ttu/json-flatfile-datastore) [![Build status](https://ci.appveyor.com/api/projects/status/adq9as6ruraln8tn?svg=true)](https://ci.appveyor.com/project/ttu/json-flatfile-datastore) [![NuGet](https://img.shields.io/nuget/v/JsonFlatFileDataStore.svg)](https://www.nuget.org/packages/JsonFlatFileDataStore/)

Simple flat file JSON datastore.

* Small API with basic functionality that is needed for handling data.
* Works with dynamic and typed data.
* Synchronous and asynchronous methods.
* Data is stored in a JSON file. 
  * Easy to initialize.
  * Easy to edit.
* [.NET Standard 1.4](https://github.com/dotnet/standard/blob/master/docs/versions.md)
  * .NET Core 1.0
  * .NET 4.6.1

###### Who should use this?

Anyone who needs to store data and wants to edit it easily with any text editor.

##### Example project

[Fake JSON Server](https://github.com/ttu/dotnet-fake-json-server) is a .NET Core Web API which uses JSON Flat File Datastore with dynamic data.

## Functionality

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

NOTE: AsQueryable will return IEnumerable, insted of IQueryable, because IQueryable doesn't support dynamic's in LINQ queries. With this datastore it won't matter as all data is already loaded into memory.

`AsQueryable` LINQ query with dynamic data:

```csharp
var store = new DataStore(pathToJson);

var collection = store.GetCollection("user");

// Find item with name
var userDynamic = collection
                    .AsQueryable()
                    .Single(p => p.name == "Phil");
```

`AsQueryable` LINQ query with typed data:

```csharp

var store = new DataStore(pathToJson);

var collection = store.GetCollection<User>();

// Find item with name
var userTyped = collection
                    .AsQueryable()
                    .Single(p => p.Name == "Phil");
```

#### Insert

`InsertOne` and `InsertOneAsync` will insert a new item to the collection. Method returns true if insert was succesful.

```csharp
// Asynchronous method and dynamic data
// Before update : { }
// After update  : { "id": 3, "name": "Raymond", "age": 32, "city" = "NY" }
await collection.InsertOneAsync(new { id = 3, name = "Raymond", age = 32, city = "NY" });

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

#### Replace

`ReplaceOne` and `ReplaceOneAsync` will replace the first item that matches the filter. Method will return true if item(s) found with the filter.

```csharp
// Sync and dynamic
// Before update : { "id": 3, "name": "Raymond", "age": 32, "city": "NY" }
// After update  : { "id": 3, "name": "Barry", "age": 42 }
collection.ReplaceOne(e => e.id == 3, new { id = 3, name = "Barry", age = 33 });

// Async and typed
// Before update : { "id": 3, "name": "Raymond", "age": 32, "city": "NY" }
// After update  : { "id": 3, "name": "Barry", "age": 42 }
await collection.ReplaceOneAsync(e => e.Id == 3, new User { Id = 3, Name = "Barry", Age = 33 });
```

`ReplaceMany` and `ReplaceManyAsync` will replace all items that match the filter.

```csharp
collection.ReplaceMany(e => e.City == "NY", new { City = "New York" });
```

#### Update

`UpdateOne` and `UpdateOneAsync` will update the first item that matches the filter with passed properties from dynamic object. Dynamic object can be an Anonymous type or an ExpandoObject. Method will return true if item(s) found with the filter.

```csharp
// Dynamic
// Before update : { "id": 1, "name": "Barry", "age": 33 }
// After update  : { "id": 1, "name": "Barry", "age": 42 }
dynamic source = new ExpandoObject();
source.age = 42;
await collection.UpdateOneAsync(e => e.id == 3, source as object);

// Typed
// Before update : { "id": 1, "name": "Phil", "age": 40, "city": "NY" }
// After update  : { "id": 1, "name": "Phil", "age": 42, "city": "NY" }
await collection.UpdateOneAsync(e => e.Name == "Phil", new { age = 42 });
```

`UpdateMany` and `UpdateManyAsync` will update all items that match the filter.

```csharp
await collection.UpdateManyAsync(e => e.Age == 30, new { age = 31 });
```

Update can also update items from the collection and add new items to the collection. Null items in the passed update data are skipped, so with null items data in the correct index can be updated.

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

Easy way to create a patch ExpandoObject on runtime is to crete a dictionary and then to serialize it to a JSON and deserialize to an ExpandoObject.

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

Dictionaries won't work when serializing JSON or data to ExpandoObjects. This is becauses dictionaries and objects are similiar when serialized to JSON, so serialization creates an ExpandoObject from Dictionary. Update's are mainly meant to be used with HTTP PATCH, so normally Replace is easier and better way to update data. 

If the update Expando is created manually then Dictionaries content can be updated. Unlike List, Dictionary's whole content is replaced with the update data's content.
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

`DeleteOne` and `DeleteOneAsync` will remove the first object that matches the filter. Method returns true if item(s) found with the filter.

```csharp
// Dynamic
await collection.DeleteOneAsync(e => e.id == 3);

// Typed
await collection.DeleteOneAsync(e => e.Id == 3);
```

`DeleteMany` and `DeleteManyAsync` will delete all items that match the filter. Method returns true if item(s) found with the filter.

```csharp
// Dynamic
await collection.DeleteManyAsync(e => e.city == "NY");

// Typed
await collection.DeleteManyAsync(e => e.City == "NY");
```

### Id-field value


If incrementing id-field values is used, `GetNextIdValue` returns next id-field value. If id-property is integer, last item's value is incremented by one. If field is not an integer, it is converted to a string and number is parsed from the end of the string and incremented by one.

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

## DataStore and Collection lifecycle

When datastore is created, it serializes the JSON file to the memory. When collection is created it has a lazy reference to the data and it will read the data when it is needed for the first time.

All write operations in collections are executed immediately internally and then the same operation is queued on DataStore's BlockingCollection. Operations are executed on backgound thread to DataStore's internal collection and saved to file. DataStore's internal collection is also updated during the save operation. NOTE: Already initialiezd collections may still have old data.

```csharp
// Data is loaded from the file
var store = new DataStore(newFilePath);

// Lazy reference to the data is created 
var collection1st = store.GetCollection("hello");
var collection2nd = store.GetCollection("hello");

// Data is loaded from the store to the collection and new item is inserted
collection1st.InsertOne(new { id = "hello" });

// Data is loaded from the store to the collection and new item is inserted
// This collection will also have item with id hello2
collection2nd.InsertOne(new { id = "hello2" });

// collection1st won't have item with id hello
```

If multiple DataStores are initialized and used simultaneously, each DataStore will have own internal state. They might become out of sync with the state in the JSON file, as data is only loaded from the file when DataStore is initialized and after each commit.

If JSON Flat File Datastore is used with e.g. Web API, add DataStore to the DI container as a singleton. This way DataStore's internal state is correct and application does not have to rely on the state on the file.

### Collection naming

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

### Writing to file

By default JSON is written in lower camel case. This can be changed with useLowerCamelCase parameter in DataStore's constructor.

```csharp
// This will write JSON in lower camel case
// e.g. { "myMovies" : [ { "longName": "xxxxx" } ] }
var store = new DataStore(newFilePath);

// This will write JSON in upper camel case
// e.g. { "MyMovies" : [ { "LongName": "xxxxx" } ] }
var store = new DataStore(newFilePath, false);
```


### Dynamic and error CS1977

This is the message you will see if you try to use dynamic types with lambdas:

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

### API

API is almost identical to the MongoDB's C# API, so switching to the MongoDB or [DocumentDB](https://docs.microsoft.com/en-us/azure/documentdb/documentdb-protocol-mongodb) might be easy. Use type inference as types are not interchangable.

* [MongoDB-C#-linq](http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/linq/#queryable)
* [MongoDB-C#-crud](http://mongodb.github.io/mongo-csharp-driver/2.4/reference/driver/crud/writing/)

### License

Licensed under the [MIT](LICENSE) License.

### Changelog

[Changelog](CHANGELOG.md)
