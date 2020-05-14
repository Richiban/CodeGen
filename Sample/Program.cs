using System;
using System.ComponentModel.DataAnnotations;
using BuilderCommon;
using Newtonsoft.Json;

namespace Sample
{
    [GenerateBuilder]
    public partial class Person
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public DateTime? BirthDate { get; private set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var myDataClass = new Person.Builder
            {
                FirstName = "Alex",
                LastName = "Foos"
            }
                .Build();

            Console.WriteLine(JsonConvert.SerializeObject(myDataClass, Formatting.Indented));

            Console.WriteLine("==== Builder Validation ====");
            try
            {
                new Person.Builder
                {
                    FirstName = "John"
                }
                    .Build();
            }
            catch (Person.Builder.ValidationException ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}

