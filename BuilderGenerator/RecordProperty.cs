namespace BuilderGenerator
{
    public class RecordProperty
    {
        public RecordProperty(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public string Type { get; }
    }
}