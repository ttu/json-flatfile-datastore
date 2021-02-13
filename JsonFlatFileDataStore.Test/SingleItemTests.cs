using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class SingleItemTests
    {
        [Fact]
        public void GetItem_DynamicAndTyped()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var itemDynamic = store.GetItem("myUser");
            var itemTyped = store.GetItem<User>("myUser");

            Assert.Equal("Hank", itemDynamic.name);
            Assert.Equal("SF", itemDynamic.work.location);
            Assert.Equal("Hank", itemTyped.Name);
            Assert.Equal("SF", itemTyped.Work.Location);
            Assert.Equal(6.7, itemDynamic.value);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void GetItem_DynamicAndTyped_SimpleType()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var itemDynamic = store.GetItem("myValue");
            var itemTyped = store.GetItem<double>("myValue");

            Assert.Equal(2.1, itemDynamic);
            Assert.Equal(2.1, itemTyped);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void GetItem_DynamicAndTyped_ArrayType()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var itemDynamic = store.GetItem("myValues");
            var itemTyped = store.GetItem<List<double>>("myValues");

            Assert.Equal(3, itemDynamic.Count);
            Assert.Equal(2.1, itemDynamic[0]);
            Assert.Equal(3, itemTyped.Count);
            Assert.Equal(2.1, itemTyped.First());

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void GetItem_DynamicAndTyped_DateType()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var test = DateTime.Now.ToShortDateString();

            var itemDynamic = store.GetItem("myDate_string");
            var itemTyped = store.GetItem<DateTime>("myDate_string");
            Assert.Equal(2009, itemTyped.Year);

            var itemDynamic2 = store.GetItem("myDate_date");
            var itemTyped2 = store.GetItem<DateTime>("myDate_date");
            Assert.Equal(2015, itemDynamic2.Year);
            Assert.Equal(2015, itemTyped2.Year);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void GetItem_Nullable_NotFound()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = store.GetItem<int?>("notFound");
            Assert.False(result.HasValue);
            Assert.Null(result);

            var result2 = store.GetItem<Guid?>("notFound");
            Assert.False(result2.HasValue);
            Assert.Null(result2);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void NotFound_Exception()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            Assert.Throws<KeyNotFoundException>(() => store.GetItem<User>("notFound"));
            Assert.Throws<KeyNotFoundException>(() => store.GetItem<Guid>("notFound"));

            Assert.Null(store.GetItem("notFound"));
            Assert.Null(store.GetItem<Guid?>("notFound"));

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void InsertItem_TypedUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = store.InsertItem("myUser2", new User { Id = 12, Name = "Teddy" });
            Assert.True(result);

            var user = store.GetItem<User>("myUser2");
            Assert.Equal("Teddy", user.Name);

            var store2 = new DataStore(newFilePath);

            var user2 = store2.GetItem<User>("myUser2");
            Assert.Equal("Teddy", user2.Name);

            var userDyanmic = store2.GetItem("myUser2");
            Assert.Equal("Teddy", userDyanmic.name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task InsertItem_DynamicUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = await store.InsertItemAsync("myUser2", new { id = 12, name = "Teddy" });
            Assert.True(result);

            var user = store.GetItem("myUser2");
            Assert.Equal("Teddy", user.name);

            var store2 = new DataStore(newFilePath);

            var user2 = store2.GetItem("myUser2");
            Assert.Equal("Teddy", user2.name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateItem_DynamicUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = await store.InsertItemAsync("myUser2", new { id = 12, name = "Teddy", Value = 2.1 });
            Assert.True(result);

            var user = store.GetItem("myUser2");
            Assert.Equal("Teddy", user.name);
            Assert.Equal(2.1, user.value);

            var updateResult = await store.UpdateItemAsync("myUser2", new { name = "Harold" });

            var store2 = new DataStore(newFilePath);

            var user2 = store2.GetItem("myUser2");
            Assert.Equal("Harold", user2.name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateItem_TypedUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = await store.InsertItemAsync("myUser2", new User { Id = 12, Name = "Teddy" });
            Assert.True(result);

            var user = store.GetItem<User>("myUser2");
            Assert.Equal("Teddy", user.Name);

            var updateResult = await store.UpdateItemAsync("myUser2", new { name = "Harold" });

            var store2 = new DataStore(newFilePath);

            var user2 = store2.GetItem<User>("myUser2");
            Assert.Equal("Harold", user2.Name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateItem_ValueType()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = await store.InsertItemAsync("counter", 2);
            Assert.True(result);

            var counter = store.GetItem<int>("counter");
            Assert.Equal(2, counter);

            var updateResult = await store.UpdateItemAsync("counter", "2");
            Assert.True(result);

            var store2 = new DataStore(newFilePath);

            var c2 = store2.GetItem("counter");
            Assert.Equal("2", c2);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task ReplaceItem_ValueType()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = await store.ReplaceItemAsync("counter", 2);
            Assert.False(result);

            result = await store.ReplaceItemAsync("counter", 2, true);
            Assert.True(result);

            var counter = store.GetItem<int>("counter");
            Assert.Equal(2, counter);

            var updateResult = await store.ReplaceItemAsync<string>("counter", "2");
            Assert.True(updateResult);

            var store2 = new DataStore(newFilePath);

            var c2 = store2.GetItem("counter");
            Assert.Equal("2", c2);

            updateResult = await store2.ReplaceItemAsync("counter", "4");
            Assert.True(updateResult);

            c2 = store2.GetItem("counter");
            Assert.Equal("4", c2);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task DeleteItem_DynamicUser_NotFound()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath, reloadBeforeGetCollection: true);

            var result = await store.DeleteItemAsync("myUser2");
            Assert.False(result);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task UpdateItem_DynamicUser_NotFound()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath, reloadBeforeGetCollection: true);

            var result = await store.UpdateItemAsync("myUser2", new { name = "James" });
            Assert.False(result);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task ReplaceItem_DynamicUser_NotFound()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = await store.ReplaceItemAsync("myUser2", new { id = 2, name = "James" });
            Assert.False(result);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task ReplaceItem_DynamicUser_Upsert()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = await store.ReplaceItemAsync("myUser2", new { id = 2, name = "James" }, true);
            Assert.True(result);

            var user = store.GetItem("myUser2");
            Assert.Equal("James", user.name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task ReplaceItem_TypedUser_Upsert()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = await store.ReplaceItemAsync("myUser2", new User { Id = 2, Name = "James" }, true);
            Assert.True(result);

            var user = store.GetItem<User>("myUser2");
            Assert.Equal("James", user.Name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task ReplaceItem_DynamicUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = store.InsertItem("myUser2", new { id = 12, name = "Teddy" });
            Assert.True(result);

            result = await store.ReplaceItemAsync("myUser2", new { id = 2, name = "James" });
            Assert.True(result);

            var user = store.GetItem("myUser2");
            Assert.Equal("James", user.name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public async Task ReplaceItem_TypedUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var result = store.InsertItem<User>("myUser2", new User { Id = 12, Name = "Teddy" });
            Assert.True(result);

            result = await store.ReplaceItemAsync("myUser2", new User { Id = 2, Name = "James" });
            Assert.True(result);

            var user = store.GetItem("myUser2");
            Assert.Equal("James", user.name);

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void DeleteItem_DynamicUser()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath, reloadBeforeGetCollection: true);

            var user = store.GetItem("myUser2");
            Assert.Null(user);

            var result = store.InsertItem("myUser2", new { id = 12, name = "Teddy" });
            Assert.True(result);

            user = store.GetItem("myUser2");
            Assert.Equal("Teddy", user.name);

            var store2 = new DataStore(newFilePath);

            var deleteResult = store2.DeleteItem("myUser2");
            Assert.True(deleteResult);

            var user2 = store2.GetItem("myUser2");
            Assert.Null(user2);

            user = store.GetItem("myUser2");
            Assert.Null(user);

            UTHelpers.Down(newFilePath);
        }
    }
}