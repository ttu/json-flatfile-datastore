using System;
using System.Collections.Generic;

namespace JsonFlatFileDataStore.Test
{
    public class TestModelWithStringArray
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public List<string> Fragments { get; set; }
    }

    public class TestModelWithIntArray
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public List<int> Fragments { get; set; }
    }

    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public string Location { get; set; }

        public WorkPlace Work { get; set; }
    }

    public class WorkPlace
    {
        public string Name { get; set; }

        public string Location { get; set; }
    }

    public class Movie
    {
        public string Name { get; set; }

        public double Rating { get; set; }
    }

    public class PrivateOwner
    {
        public string FirstName { get; set; }

        public string OwnerLongTestProperty { get; set; }

        public List<int> MyValues { get; set; }

        public Dictionary<string, string> MyStrings { get; set; }

        public Dictionary<int, int> MyIntegers { get; set; }
    }

    public class Family
    {
        public int Id { get; set; }

        public string FamilyName { get; set; }

        public IList<Parent> Parents { get; set; }

        public IList<Child> Children { get; set; }

        public Address Address { get; set; }

        public BankAccount BankAccount { get; set; }

        public string Notes { get; set; }
    }

    public class Parent
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Gender Gender { get; set; }

        public int Age { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public Work Work { get; set; }

        public string FavouriteMovie { get; set; }
    }

    public enum Gender
    {
        Male,
        Female
    }

    public class Work
    {
        public string CompanyName { get; set; }

        public string Address { get; set; }
    }

    public class Child
    {
        public string Name { get; set; }

        public Gender Gender { get; set; }

        public int Age { get; set; }

        public List<Friend> Friends { get; set; }
    }

    public class Friend
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }

        public int PostNumber { get; set; }

        public string City { get; set; }

        public int Age { get; set; }

        public Country Country { get; set; }
    }

    public class Country
    {
        public string Name { get; set; }

        public int Code { get; set; }
    }

    public class BankAccount
    {
        public DateTime Opened { get; set; }

        public string Balance { get; set; }

        public bool Active { get; set; }
    }
}