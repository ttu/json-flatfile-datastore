using System;
using System.Collections.Generic;

namespace JsonFlatFileDataStore.Test
{
    internal class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public string Location { get; set; }

        public WorkPlace Work { get; set; }
    }

    internal class WorkPlace
    {
        public string Name { get; set; }

        public string Location { get; set; }
    }

    internal class Movie
    {
        public string Name { get; set; }
    }

    internal class PrivateOwner
    {
        public string FirstName { get; set; }

        public string OwnerLongTestProperty { get; set; }

        public List<int> MyValues { get; set; }

        public Dictionary<string, string> MyStrings { get; set; }

        public Dictionary<int, int> MyIntegers { get; set; }
    }

    internal class Family
    {
        public int Id { get; set; }

        public string FamilyName { get; set; }

        public IList<Parent> Parents { get; set; }

        public IList<Child> Children { get; set; }

        public Address Address { get; set; }

        public BankAccount BankAccount { get; set; }

        public string Notes { get; set; }
    }

    internal class Parent
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Gender Gender { get; set; }

        public int Age { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public Work Workplace { get; set; }

        public string FavouriteMovie { get; set; }
    }

    public enum Gender
    {
        Male,
        Female
    }

    internal class Work
    {
        public string CompanyName { get; set; }

        public string Address { get; set; }
    }

    internal class Child
    {
        public string Name { get; set; }

        public Gender Gender { get; set; }

        public int Age { get; set; }

        public List<Friend> Friends { get; set; }
    }

    internal class Friend
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }

    internal class Address
    {
        public string Street { get; set; }

        public int PostNumber { get; set; }

        public string City { get; set; }

        public int Age { get; set; }

        public Country Country { get; set; }
    }

    internal class Country
    {
        public string Name { get; set; }

        public int Code { get; set; }
    }

    internal class BankAccount
    {
        public DateTime Opened { get; set; }

        public string Balance { get; set; }

        public bool Active { get; set; }
    }
}