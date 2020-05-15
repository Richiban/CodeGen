using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BuilderGenerator
{
    interface IPatternGenerator
    {
        (string name, string content) GenerateSrcFileContent(ClassDeclarationSyntax c, string usings);
    }
}