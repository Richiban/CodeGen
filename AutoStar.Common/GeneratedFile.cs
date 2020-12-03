using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using AutoStar.Model;
using System.Text;

namespace AutoStar.Common
{
    public class GeneratedFile
    {
        public GeneratedFile(TypeFile typeFile, string generatorName)
        {
            TypeFile = typeFile;
            GeneratorName = generatorName;
        }

        public string GeneratorName { get; }
        public TypeFile TypeFile { get; }

        public void AddTo(GeneratorExecutionContext context)
        {
            var typeName = TypeFile.TypeDeclaration.Name;

            var codeBuilder = new CodeBuilder();

            TypeFile.WriteTo(codeBuilder);

            context.AddSource($"{typeName}.{GeneratorName}.g.cs",
                SourceText.From(codeBuilder.ToString(), Encoding.UTF8));
        }
    }
}