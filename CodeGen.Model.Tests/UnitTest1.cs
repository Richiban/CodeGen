using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using Richiban.CodeGen.BuilderPattern;
using System;
using System.Linq;

namespace CodeGen.Tests
{
    public class BuilderPatternGeneratorTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var sut = new BuilderPatternGenerator();
            var tree = CSharpSyntaxTree.ParseText(sampleCode);
            var root = (CompilationUnitSyntax)tree.GetRoot();
            var usings = root.Usings.ToString();

            var ns = root.ChildNodes().OfType<NamespaceDeclarationSyntax>().Single();

            var classDeclaration = ns.ChildNodes().OfType<ClassDeclarationSyntax>().Single();

            var result = sut.GenerateSrcFileContent(classDeclaration, usings);

            Console.WriteLine(result);
        }

        private const string sampleCode = @"
using System;

namespace Test
{
    public partial class RecordSubject
    {
        public string A { get; }
        public string? B { get; }
        public string C { get; } = """";
    }
}
"
    ;
    }

}