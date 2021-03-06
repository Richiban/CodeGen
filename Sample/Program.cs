﻿using System;
using System.ComponentModel.DataAnnotations;
using BuilderCommon;
using Newtonsoft.Json;

namespace Sample
{
    [RecordBuilder]
    public partial class Person
    {
        public string FirstName { get; }
        public string LastName { get; }
        public DateTime? BirthDate { get; }
        public string? A { get; }
        public string B { get; } = "B val";
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==== Builder Validation ====");

            try
            {
                var a = new Person.Builder
                {
                    FirstName = "Alex",
                    LastName = "Bloggs"
                }.Build();

                var b = new Person.Builder(a).Build();

                Console.WriteLine($"a = {a}");

                Console.WriteLine($"b = {b}");

                Console.WriteLine($"ReferenceEquals: {ReferenceEquals(a, b)}");

                Console.WriteLine($"Equals: {Equals(a, b)}");

                var c = new Person.Builder(b)
                {
                    LastName = "Blobbs"
                }.Build();

                Console.WriteLine($"c = {c}");

                Console.WriteLine($"ReferenceEquals: {ReferenceEquals(b, c)}");

                Console.WriteLine($"Equals: {Equals(b, c)}");
            }
            catch (Person.Builder.ValidationException ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}

