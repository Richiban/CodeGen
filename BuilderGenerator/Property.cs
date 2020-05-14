namespace BuilderGenerator
{
    public class Property : IWriteableCode
    {
        public Property(string name, string type, bool hasSetter, Visibility visibility)
        {
            Name = name;
            Type = type;
            HasSetter = hasSetter;
            Visibility = visibility;
        }

        public string Name { get; }
        public string Type { get; }
        public bool HasSetter { get; }
        public Visibility Visibility { get; }

        public void WriteTo(CodeBuilder codeBuilder)
        {
            Visibility.WriteTo(codeBuilder);
            codeBuilder.Append($"{Type} {Name}");

            if (HasSetter)
                codeBuilder.AppendLine(" { get; set; }");
            else
                codeBuilder.AppendLine(" { get; }");
        }
    }
}