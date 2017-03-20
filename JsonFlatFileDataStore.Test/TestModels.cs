using System.Collections.Generic;

namespace JsonFlatFileDataStore.Test
{
    internal class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public string Location { get; set; }
    }

    internal class Movie
    {
        public string Name { get; set; }
    }

    internal class Family
    {
        public string Id { get; set; }

        public List<Parent> Parents { get; set; }

        public Address Address { get; set; }
    }

    internal class Parent
    {
        public string FirstName { get; set; }

        public int Age { get; set; }
    }

    internal class Address
    {
        public string City { get; set; }
    }

    internal class Owner
    {
        public string FirstName { get; set; }

        public List<int> MyValues { get; set; }
    }
}