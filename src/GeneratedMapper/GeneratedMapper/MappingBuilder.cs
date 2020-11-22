﻿using GeneratedMapper.Configurations;
using Microsoft.CodeAnalysis.Text;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GeneratedMapper
{
    internal sealed class MappingBuilder
    {
        private const string SourceInstanceName = "self";

        public MappingBuilder(MappingInformation information, ConfigurationValues configurationValues)
        {
            Text = Build(information, configurationValues);
        }

        private static SourceText Build(MappingInformation information, ConfigurationValues configurationValues)
        {
            using var writer = new StringWriter();
            using var indentWriter = new IndentedTextWriter(writer,
                configurationValues.IndentStyle == IndentStyle.Tab ? "\t" : new string(' ', (int)configurationValues.IndentSize));

            var usingStatements = new SortedSet<string>();

            if (!information.SourceType.IsValueType)
            {
                usingStatements.Add("using System;");
            };

            if (!information.DestinationType.ContainingNamespace.IsGlobalNamespace &&
                !information.SourceType.ContainingNamespace.ToDisplayString().StartsWith(
                    information.DestinationType.ContainingNamespace.ToDisplayString(), StringComparison.InvariantCulture))
            {
                usingStatements.Add($"using {information.DestinationType.ContainingNamespace.ToDisplayString()};");
            }

            foreach (var usingStatement in usingStatements)
            {
                indentWriter.WriteLine(usingStatement);
            }

            if (usingStatements.Count > 0)
            {
                indentWriter.WriteLine();
            }

            if (!information.SourceType.ContainingNamespace.IsGlobalNamespace)
            {
                indentWriter.WriteLine($"namespace {information.SourceType.ContainingNamespace.ToDisplayString()}");
                indentWriter.WriteLine("{");
                indentWriter.Indent++;
            }

            indentWriter.WriteLine($"public static partial class {information.SourceType.Name}MapToExtensions");
            indentWriter.WriteLine("{");
            indentWriter.Indent++;

            indentWriter.WriteLine($"public static {information.DestinationType.Name} MapTo{information.DestinationType.Name}(this {information.SourceType.Name} {SourceInstanceName})");
            indentWriter.WriteLine("{");
            indentWriter.Indent++;

            if (!information.SourceType.IsValueType)
            {
                indentWriter.WriteLine($"if ({SourceInstanceName} is null)");
                indentWriter.WriteLine("{");
                indentWriter.Indent++;
                indentWriter.WriteLine("throw new ArgumentNullException(nameof(self));");
                indentWriter.Indent--;
                indentWriter.WriteLine("}");
                indentWriter.WriteLine("");
            }

            indentWriter.WriteLine($"return new {information.DestinationType.Name}");
            indentWriter.WriteLine("{");
            indentWriter.Indent++;

            foreach (var map in information.Mappings)
            {
                var initializerString = map.InitializerString(SourceInstanceName);
                if (initializerString != null)
                {
                    indentWriter.WriteLine(initializerString);
                }
            }

            indentWriter.Indent--;
            indentWriter.WriteLine("};");
            indentWriter.Indent--;
            indentWriter.WriteLine("}");
            indentWriter.Indent--;
            indentWriter.WriteLine("}");

            if (!information.SourceType.ContainingNamespace.IsGlobalNamespace)
            {
                indentWriter.Indent--;
                indentWriter.WriteLine("}");
            }

            return SourceText.From(writer.ToString(), Encoding.UTF8);
        }

        public SourceText Text { get; private set; }
    }
}