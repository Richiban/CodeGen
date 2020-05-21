using BuilderGenerator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
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

            var (name, content) = sut.GenerateSrcFileContent(classDeclaration, usings);

            Console.WriteLine(content);
        }

        private const string sampleCode = @"
using System;

namespace Test
{
    public partial class Person
    {
        public string FirstName { get; }
        public string LastName { get; }
        public DateTime? BirthDate { get; }
        public string? A { get; }
        public string B { get; } = ""B val"";
    }
}
"
    ;
    }

}