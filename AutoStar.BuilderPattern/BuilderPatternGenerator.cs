using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AutoStar.Common;
using AutoStar.Model;

namespace AutoStar.BuilderPattern
{
    [Generator]
    public class BuilderPatternGenerator : CodePatternSourceGeneratorBase
    {
        protected override string GeneratorName => nameof(BuilderPattern);

        public override GeneratedFile GeneratePatternFor(ClassDeclarationSyntax classDeclaration, string usings, string @namespace)
        {
            var converter = new RecordConverter();

            var record = converter.ClassToRecord(classDeclaration);

            var recordClass = converter.LowerRecordToClass(record);

            var typeFile = new TypeFile(usings, recordClass)
            {
                NamespaceName = @namespace
            };

            return new GeneratedFile(typeFile, GeneratorName);
        }
    }
}
