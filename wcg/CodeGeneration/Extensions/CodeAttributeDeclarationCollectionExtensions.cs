using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.CSharp;

namespace wcg.CodeGeneration.Extensions
{
    internal static class CodeAttributeDeclarationCollectionExtensions
    {
        public static IEnumerable<CodeAttributeDeclaration> AllAttributes(this CodeAttributeDeclarationCollection attributes)
        {
            return attributes.OfType<CodeAttributeDeclaration>();
        }

        public static CodeAttributeDeclaration FirstOrDefault(this CodeAttributeDeclarationCollection attributes, Func<CodeAttributeDeclaration, bool> condition)
        {
            return attributes.AllAttributes().FirstOrDefault(condition);
        }

        public static void AddOrReplace(this CodeAttributeDeclarationCollection attributes, CodeAttributeDeclaration replacement)
        {
            var attr = attributes.FirstOrDefault(a => a.Name == replacement.Name);
            if (attr != null)
            {
                attributes.Remove(attr);
            }

            attributes.Add(replacement);
        }

        public static void AddIfNotPresent(this CodeAttributeDeclarationCollection attributes, CodeAttributeDeclaration replacement)
        {
            var attr = attributes.FirstOrDefault(a => a.Name == replacement.Name);
            if (attr == null)
            {
                attributes.Add(replacement);
            }
        }

        public static string GenerateSourceCode(this CodeAttributeDeclarationCollection attributes, string indentation = "        ")
        {
            StringBuilder generatedAttributes = new StringBuilder();
            foreach (CodeAttributeDeclaration declaration in attributes)
            {
                generatedAttributes.Append(indentation);
                generatedAttributes.Append("[");
                generatedAttributes.Append(declaration.Name.EscapeNamespace());
                if (declaration.Arguments.Count > 0)
                {
                    generatedAttributes.Append("(");
                    bool first = true;

                    foreach (CodeAttributeArgument argument in declaration.Arguments)
                    {
                        if (!first)
                        {
                            generatedAttributes.Append(", ");
                        }
                        else
                        {
                            first = false;
                        }

                        if (!string.IsNullOrEmpty(argument.Name))
                        {
                            generatedAttributes.Append(CodeIdentifier.MakeValid(argument.Name));
                            generatedAttributes.Append(" = ");
                        }

                        using (var stringWriter = new StringWriter(generatedAttributes))
                        {
                            new CSharpCodeProvider().GenerateCodeFromExpression(argument.Value, stringWriter, new CodeGeneratorOptions());
                        }
                    }

                    generatedAttributes.Append(")");
                }
                generatedAttributes.AppendLine("]");
            }

            return generatedAttributes.ToString();
        }
    }
}
