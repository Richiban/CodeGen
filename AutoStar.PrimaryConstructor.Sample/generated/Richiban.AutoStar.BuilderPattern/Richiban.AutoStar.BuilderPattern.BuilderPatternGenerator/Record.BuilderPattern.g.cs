using System;
    public partial class Record
    {
        public Record(string  Data)
        {
            this.Data = Data;
        }
        public override string ToString()
        {
            var elements = new string[] { $"Data = {Data}" };
            var s = System.String.Join(", ", elements);
            return $"Record {{ {s} }}";
        }
        public class Builder
        {
            public string  Data { get; set; }
            public void Validate()
            {
                var errors = new System.Collections.Generic.List<string>();
                if (Data is null) errors.Add("Data is null");
                if (errors.Count > 0) throw new ValidationException(errors);
            }
            public Record Build()
            {
                Validate();
                return new Record(Data);
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
