using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class ItemTests
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

            UTHelpers.Down(newFilePath);
        }

        [Fact]
        public void FindAndAsQueryable_Linq()
        {
            var newFilePath = UTHelpers.Up();

            var store = new DataStore(newFilePath);

            var itemDynamic = store.GetItem("myValue");
            var itemTyped = store.GetItem<double>("myValue");

            Assert.Equal(2.1, itemDynamic);
            Assert.Equal(2.1, itemTyped);

            UTHelpers.Down(newFilePath);
        }
    }
}