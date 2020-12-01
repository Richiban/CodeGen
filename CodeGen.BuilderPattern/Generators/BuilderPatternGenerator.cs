using Microsoft.CodeAnalysis.CSharp.Syntax;
using Richiban.CodeGen.Model;

namespace Richiban.CodeGen.BuilderPattern
{
    public class BuilderPatternGenerator : IPatternGenerator
    {
        public (string name, string content) GenerateSrcFileContent(ClassDeclarationSyntax classDeclaration, string usings)
        {
            var namespaceName = classDeclaration
                    .FindParent<NamespaceDeclarationSyntax>()
                    ?.Name.ToString();

            var converter = new RecordConverter();

            var record = converter.ClassToRecord(classDeclaration);

            var cb = new CodeBuilder();

            var recordClass = converter.RecordToClass(record);

            var output = new ClassFile(usings, recordClass)
            { 
                NamespaceName = namespaceName
            };

            output.WriteTo(cb);

            return ($"{recordClass.Name}.Builder", cb.ToString());
        }
    }
}
