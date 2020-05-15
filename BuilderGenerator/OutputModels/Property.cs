using System.ComponentModel;

namespace BuilderGenerator
{
    public class Property : IWriteableCode
    {
        public Property(string name, string type, bool hasSetter, Visibility visibility, string? defaultValue)
        {
            Name = name;
            Type = type;
            HasSetter = hasSetter;
            Visibility = visibility;
            DefaultValue = defaultValue;
        }

        public string Name { get; }
        public string Type { get; }
        public bool HasSetter { get; }
        public Visibility Visibility { get; }
        public string? DefaultValue { get; }

        public void WriteTo(CodeBuilder codeBuilder)
        {
            Visibility.WriteTo(codeBuilder);
            codeBuilder.Append($"{Type} {Name}");

            if (HasSetter)
                codeBuilder.Append(" { get; set; }");
            else
                codeBuilder.Append(" { get; }");

            if (DefaultValue != null)
            {
                codeBuilder.Append($" {DefaultValue}");
            }

            codeBuilder.AppendLine("");
        }
    }
}