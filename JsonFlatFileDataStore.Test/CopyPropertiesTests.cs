using System.Collections.Generic;
using System.Dynamic;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class CopyPropertiesTests
    {
        [Fact]
        public void CopyProperties_TypedAndDynamicAddressCity()
        {
            var family = new Family { Address = new Address { City = "Helsinki" } };

            ObjectExtensions.CopyProperties(new { City = "Espoo" }, family.Address);
            Assert.Equal("Espoo", family.Address.City);

            ObjectExtensions.CopyProperties(new { Address = new { City = "Turku" } }, family);
            Assert.Equal("Turku", family.Address.City);
        }

        [Fact]
        public void CopyProperties_TypedFamily()
        {
            var family = new Family
            {
                Parents = new List<Parent>
                {
                    new Parent {  FirstName = "Jim", Age = 52 },
                    new Parent {  FirstName = "Theodor", Age = 14 }
                },
                Address = new Address { City = "Helsinki" }
            };

            ObjectExtensions.CopyProperties(new { Parents = new[] { new { FirstName = "Ray", Age = 49 } } }, family);
            Assert.Equal(49, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(14, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);

            ObjectExtensions.CopyProperties(new { Parents = new[] { new { Age = 39 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(14, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);

            ObjectExtensions.CopyProperties(new { Parents = new[] { null, new { Age = 21 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(21, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);

            ObjectExtensions.CopyProperties(new { Parents = new[] { null, null, new { FirstName = "Bill", Age = 28 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(21, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);
            Assert.Equal(28, family.Parents[2].Age);
            Assert.Equal("Bill", family.Parents[2].FirstName);

            Assert.Equal("Helsinki", family.Address.City);
        }

        [Fact]
        public void CopyProperties_DynamicFamily()
        {
            dynamic family = new ExpandoObject();

            dynamic fParent = new ExpandoObject();
            fParent.FirstName = "Jim";
            fParent.Age = 52;

            dynamic sParent = new ExpandoObject();
            sParent.FirstName = "Theodor";
            sParent.Age = 14;

            family.Parents = new List<ExpandoObject>
            {
                fParent,
                sParent,
            };

            family.Address = new ExpandoObject();
            family.Address.City = "Helsinki";

            ObjectExtensions.CopyProperties(new { Parents = new[] { new { FirstName = "Ray", Age = 49 } } }, family);
            Assert.Equal(49, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(14, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);

            ObjectExtensions.CopyProperties(new { Parents = new[] { new { Age = 39 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(14, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);

            ObjectExtensions.CopyProperties(new { Parents = new[] { null, new { Age = 21 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(21, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);

            ObjectExtensions.CopyProperties(new { Parents = new[] { null, null, new { FirstName = "Bill", Age = 28 } } }, family);
            Assert.Equal(39, family.Parents[0].Age);
            Assert.Equal("Ray", family.Parents[0].FirstName);
            Assert.Equal(21, family.Parents[1].Age);
            Assert.Equal("Theodor", family.Parents[1].FirstName);
            Assert.Equal(28, family.Parents[2].Age);
            Assert.Equal("Bill", family.Parents[2].FirstName);

            Assert.Equal("Helsinki", family.Address.City);
        }

        [Fact]
        public void CopyProperties_TypedArrayValueTypes()
        {
            var item = new PrivateOwner();
            item.FirstName = "Theodor";
            item.MyValues = new List<int> { 1, 2, 3 };

            ObjectExtensions.CopyProperties(new { MyValues = new List<int> { 4, 5, 6, 7 } }, item);
            Assert.Equal(4, item.MyValues[0]);
            Assert.Equal(7, item.MyValues[3]);
        }

        [Fact]
        public void CopyProperties_DynamicArrayValueTypes()
        {
            dynamic item = new ExpandoObject();
            item.FirstName = "Theodor";
            item.MyValues = new List<int> { 1, 2, 3 };

            ObjectExtensions.CopyProperties(new { MyValues = new List<int> { 4, 5, 6, 7 } }, item);
            Assert.Equal(4, item.MyValues[0]);
            Assert.Equal(7, item.MyValues[3]);
        }

        [Fact]
        public void CopyProperties_TypedFamilyParents()
        {
            var family = new Family
            {
                Parents = new List<Parent>
                {
                    new Parent {  FirstName = "Jim", Age = 52 }
                },
                Address = new Address { City = "Helsinki" }
            };

            ObjectExtensions.CopyProperties(new { Age = 49 }, family.Parents[0]);
            Assert.Equal(49, family.Parents[0].Age);
            Assert.Equal("Helsinki", family.Address.City);
        }
    }
}