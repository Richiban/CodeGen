using System;
namespace Sample
{
    public partial class Person
    {
        public Person(string  FirstName, string  LastName, DateTime?  BirthDate, string?  A, string  B)
        {
            this.FirstName = FirstName;
            this.LastName = LastName;
            this.BirthDate = BirthDate;
            this.A = A;
            this.B = B;
        }
        public override string ToString()
        {
            var elements = new string[] { $"FirstName = {FirstName}",
$"LastName = {LastName}",
$"BirthDate = {BirthDate}",
$"A = {A}",
$"B = {B}" };
            var s = System.String.Join(", ", elements);
            return $"Person {{ {s} }}";
        }
        public class Builder
        {
            public string  FirstName { get; set; }
            public string  LastName { get; set; }
            public DateTime?  BirthDate { get; set; }
            public string?  A { get; set; }
            public string  B { get; set; } = "B val";
            public void Validate()
            {
                var errors = new System.Collections.Generic.List<string>();
                if (FirstName is null) errors.Add("FirstName is null");
                if (LastName is null) errors.Add("LastName is null");
                if (B is null) errors.Add("B is null");
                if (errors.Count > 0) throw new ValidationException(errors);
            }
            public Person Build()
            {
                Validate();
                return new Person(FirstName, LastName, BirthDate, A, B);
            }
            public class ValidationException : System.Exception
            {
                public ValidationException(System.Collections.Generic.IReadOnlyCollection<string> errors) : base(GetMessage(errors))
                {
                    Errors = errors;
                }
                public System.Collections.Generic.IReadOnlyCollection<string> Errors { get; }
                private static string GetMessage(System.Collections.Generic.IReadOnlyCollection<string> errors)
                {
                    return string.Join(System.Environment.NewLine, errors);
                }
            }
        }
    }
}
