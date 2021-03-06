﻿using System.Collections.Generic;
using System.Linq;

namespace BuilderGenerator
{
    public class ClassDeclaration : IWriteableCode
    {
        public ClassDeclaration(
            string name,
            IReadOnlyCollection<Constructor>? constructors = null,
            Visibility? visibility = null,
            string[] inheritsImplements = null,
            bool isPartial = false,
            params IWriteableCode[] contents)
        {
            Name = name;
            Constructors = constructors ?? new Constructor[0];
            Visibility = visibility ?? Visibility.None;
            InheritsImplements = inheritsImplements ?? new string[0];
            IsPartial = isPartial;
            Contents = contents;
        }

        public string Name { get; }
        public IReadOnlyCollection<Constructor> Constructors { get; }
        public Visibility Visibility { get; }
        public bool IsPartial { get; }
        public IReadOnlyCollection<IWriteableCode> Contents { get; }
        public string[] InheritsImplements { get; }

        public void WriteTo(CodeBuilder cb)
        {
            Visibility.WriteTo(cb);

            if (IsPartial)
                cb.Append("partial ");

            cb.Append("class ");
            cb.Append(Name);

            if (InheritsImplements?.Length > 0)
            {
                cb.Append($" : {string.Join(", ", InheritsImplements)}");
            }

            cb.AppendLine("");

            cb.AppendLine("{");

            using (cb.Indent())
            {
                cb.WriteAll(Constructors);

                foreach (var item in Contents ?? new IWriteableCode[] { })
                {
                    item.WriteTo(cb);
                }
            }

            cb.AppendLine("}");
        }
    }
}