using System;
    public partial class AutoBuilderAttribute
    {
        public AutoBuilderAttribute()
        {
        }
        public override string ToString()
        {
            var elements = new string[] {  };
            var s = System.String.Join(", ", elements);
            return $"AutoBuilderAttribute {{ {s} }}";
        }
        public class Builder
        {
            public void Validate()
            {
                var errors = new System.Collections.Generic.List<string>();
                if (errors.Count > 0) throw new ValidationException(errors);
            }
            public AutoBuilderAttribute Build()
            {
                Validate();
                return new AutoBuilderAttribute();
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
