using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.CSharp;

namespace wcg.WebFiles
{
    internal sealed class Compiland : IDisposable
    {
        public string GeneratedNamespace { get; }
        public string SourceNamespace { get; }

        public CodeDomProvider CodeProvider { get; }
        public CodeGeneratorOptions CodeGeneratorOptions { get; }
        public CodeGenerationOptions GenerationOptions { get; }
        public CodeIdentifiers CodeIdentifiers { get; }
        public ImportContext ImportContext { get; }

        public CodeNamespace CodeNamespace { get; }

        public string[] Includes { get; }

        public string InputPath { get; }

        public string OutputPath { get; }

        public Compiland(string codeNamespace, string sourceNamespace, string inputPath, string outputPath, string[] includes)
        {
            GeneratedNamespace = codeNamespace ?? string.Empty;
            SourceNamespace = sourceNamespace ?? string.Empty;
            InputPath = inputPath;
            OutputPath = outputPath;

            Includes = includes;

            CodeIdentifiers = new CodeIdentifiers();
            ImportContext = new ImportContext(CodeIdentifiers, false);
            
            CodeProvider = new CSharpCodeProvider();

            GenerationOptions = CodeGenerationOptions.GenerateProperties | CodeGenerationOptions.GenerateOldAsync | CodeGenerationOptions.GenerateNewAsync;

            CodeGeneratorOptions = new CodeGeneratorOptions
            {
                BlankLinesBetweenMembers = true,
                BracingStyle = "C",
                ElseOnClosing = false,
                IndentString = "    ",
                VerbatimOrder = false
            };
            
            CodeNamespace = new CodeNamespace(GeneratedNamespace);
        }

        public CodeCompileUnit CreateCodeUnit()
        {
            var unit = new CodeCompileUnit();
            unit.Namespaces.Add(CodeNamespace);
            return unit;
        }

        public XmlCodeExporter CreateCodeExporter()
        {
            return new XmlCodeExporter(CodeNamespace);
        }

        ~Compiland()
        {
            Dispose(false);
        }

        public void Link()
        {
            foreach (var include in Includes)
            {
                CodeNamespace.Imports.Add(new CodeNamespaceImport(include));
            }
        }

        public void WriteCode()
        {
            using (var fileStream = new FileStream(OutputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                using (var output = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    CodeProvider.GenerateCodeFromNamespace(CodeNamespace, output, CodeGeneratorOptions);
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                CodeProvider.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
