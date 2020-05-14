using System.Collections.Generic;
using System.Linq;

namespace BuilderGenerator
{
    public class ClassDec : IWriteableCode
    {
        public ClassDec(
            string name,
            Constructor constructor = null,
            Visibility visibility = null,
            string baseClass = null,
            bool isPartial = false,
            params IWriteableCode[] contents)
        {
            Name = name;
            Constructor = constructor ?? new Constructor.None();
            Visibility = visibility ?? Visibility.None;
            BaseClass = baseClass;
            IsPartial = isPartial;
            Contents = contents;
        }

        public string Name { get; }
        public Constructor Constructor { get; }
        public Visibility Visibility { get; }
        public bool IsPartial { get; }
        public IReadOnlyCollection<IWriteableCode> Contents { get; }
        public string BaseClass { get; }

        public void WriteTo(CodeBuilder cb)
        {
            Visibility.WriteTo(cb);

            if (IsPartial)
                cb.Append("partial ");

            cb.Append("class ");
            cb.Append(Name);

            if (BaseClass != null)
            {
                cb.Append($" : {BaseClass}");
            }

            cb.AppendLine("");

            cb.AppendLine("{");

            using (cb.Indent())
            {
                Constructor?.WriteTo(cb);

                foreach (var item in Contents ?? new IWriteableCode[] { })
                {
                    item.WriteTo(cb);
                }
            }

            cb.AppendLine("}");
        }
    }
}