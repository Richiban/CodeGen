using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BuilderGenerator
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

            var output = new ClassFile(usings, namespaceName, recordClass);

            output.WriteTo(cb);

            return ($"{recordClass.Name}.Builder", cb.ToString());
        }
    }
}
