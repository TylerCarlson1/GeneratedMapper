﻿using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace GeneratedMapper.Tests.Helpers
{
    public static class GeneratorTestHelper
    {
        private static (ImmutableArray<Diagnostic>, string[]) GetGeneratedOutput(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
                .Select(x => MetadataReference.CreateFromFile(x.Location))
                .Concat(new[] { MetadataReference.CreateFromFile(typeof(MapperGenerator).Assembly.Location) });
            var compilation = CSharpCompilation.Create("generator", new SyntaxTree[] { syntaxTree },
                references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var originalTreeCount = compilation.SyntaxTrees.Length;
            var generator = new MapperGenerator();

            var driver = CSharpGeneratorDriver.Create(ImmutableArray.Create<ISourceGenerator>(generator));
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            var trees = outputCompilation.SyntaxTrees.ToList();

            return (diagnostics,
                trees.Count != originalTreeCount
                    ? trees.Skip(1).Select(x => x.ToString()).ToArray()
                    : new string[0]);
        }

        public static void TestGeneratedCode(string sourceText, params string[] expectedOutputSourceTexts)
        {
            var (diagnostics, output) = GetGeneratedOutput(sourceText);

            Assert.AreEqual(0, diagnostics.Length, string.Join(", ", diagnostics.Select(x => x.GetMessage())));

            for (var i = 0; i < expectedOutputSourceTexts.Length; i++)
            {
                Assert.AreEqual(expectedOutputSourceTexts[i], output.ElementAtOrDefault(i) ?? "", $"Error in file index: {i}");
            }
        }

        public static void TestReportedDiagnostics(string sourceText, params string[] expectedDiagnosticErrors)
        {
            var (diagnostics, output) = GetGeneratedOutput(sourceText);

            var errorCodes = diagnostics.Select(x => x.Id).ToArray();

            Assert.AreEqual(expectedDiagnosticErrors.Length, diagnostics.Length, $"Found messages: {string.Join(", ", errorCodes)}.");

            foreach (var diagnostic in expectedDiagnosticErrors)
            {
                Assert.Contains(diagnostic, errorCodes);
            }
        }
    }
}
