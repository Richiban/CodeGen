using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Richiban.CodeGen.BuilderPattern
{
    interface IPatternGenerator
    {
        (string name, string content) GenerateSrcFileContent(ClassDeclarationSyntax c, string usings);
    }
}