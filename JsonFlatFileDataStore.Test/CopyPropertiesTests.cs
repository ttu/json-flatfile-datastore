using System.Collections.Generic;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
    public class CopyPropertiesTests
    {
        [Fact]
        public void Reflection_CopyProperties_AdressCity()
        {
            var family = new Family { Address = new Address { City = "Helsinki" } };

            ObjectExtensions.CopyProperties(new { City = "Espoo" }, family.Address);
            Assert.Equal("Espoo", family.Address.City);

            ObjectExtensions.CopyProperties(new { Address = new { City = "Turku" } }, family);
            Assert.Equal("Turku", family.Address.City);
        }

        [Fact]
        public void Reflection_CopyProperties_FamilyParents()
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

        // TODO: Array implementation
        public void Reflection_CopyProperties_Array()
        {
            var family = new Family
            {
                Parents = new List<Parent>
                {
                    new Parent {  FirstName = "Jim", Age = 52 }
                },
                Address = new Address { City = "Helsinki" }
            };

            ObjectExtensions.CopyProperties(new { Parents = new[] { new { Age = 48 } } }, family);
            Assert.Equal(48, family.Parents[0].Age);
        }
    }
}