using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Richiban.AutoStar.BuilderPattern;
using Richiban.AutoStar.Common;
using Richiban.AutoStar.Model;

namespace Richiban.AutoStar.AutoConstructor
{
    [Generator]
    public class AutoConstructorGenerator : CodePatternSourceGeneratorBase
    {
        protected override string GeneratorName => nameof(AutoConstructor);

        private static string ToCamelCase(string name)
        {
            name = name.TrimStart('_');

            return name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
        }

        public override GeneratedFile GeneratePatternFor(ClassDeclarationSyntax classDeclaration, string usings, string @namespace)
        {
            var fieldList = classDeclaration.Members
                .OfType<FieldDeclarationSyntax>()
                .Where(x => x.Modifiers.Any(y => y.ValueText == "readonly"))
                .Select(x => x.Declaration)
                .SelectMany(z => 
                    z.Variables.Select(aa => new { 
                        Type = z.Type.ToString(), ParameterName = ToCamelCase(aa.Identifier.ValueText), Name = aa.Identifier.ValueText }))
                .ToList();

            var parameters = fieldList.Select(it => new Parameter(it.ParameterName, it.Type)).ToList();
            var assignments = fieldList.Select(field => new AssignmentStatement($"this.{field.Name}", field.ParameterName)).ToList();
            var className = classDeclaration.Identifier.ValueText;

            var typeFile = new TypeFile(@namespace, new ClassDeclaration(className)
            {
                IsPartial = true,
                Constructor = new Constructor.BlockConstructor(className)
                {
                    Parameters = parameters,
                    Statements = assignments
                }
            });

            return new GeneratedFile(typeFile, GeneratorName);
        }
    }
}