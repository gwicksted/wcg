using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.CSharp;
using wcg.CodeGeneration.Extensions;

namespace wcg.CodeGeneration
{
    internal class ShorthandProperties : IPostProcessor
    {
        // TODO: apply to types and enum members

        private string CSharpifyName(string name)
        {
            string pascal = CodeIdentifier.MakePascal(name);

            var words = SplitPascal(pascal);

            StringBuilder adjusted = new StringBuilder();

            foreach (var word in words)
            {
                if (word.Length > 1)
                {
                    string replacement = word[0] + new string(word.Skip(1).Select(char.ToLower).ToArray());
                    adjusted.Append(replacement);
                }
                else
                {
                    adjusted.Append(word);
                }
            }

            return adjusted.ToString();
        }
        
        private IEnumerable<string> SplitPascal(string pascal)
        {
            bool allUpper = false;

            int offset = 0;

            for (int i = 0; i < pascal.Length; i++)
            {
                char c = pascal[i];

                if (char.IsUpper(c))
                {
                    if (!allUpper)
                    {
                        if (i == offset + 1)
                        {
                            allUpper = true;
                        }
                        else if (i != offset)
                        {
                            yield return pascal.Substring(offset, i - offset);
                            offset = i;
                        }
                    }
                }
                else
                {
                    if (allUpper)
                    {
                        // If upper-case word more than 2 characters in length, commit everything up to the character before this one
                        if (i - offset > 2)
                        {
                            // "URLRange" => "URLRa" => "Url" "Range"
                            i--;
                            yield return pascal.Substring(offset, i - offset);
                            offset = i;
                            allUpper = false;
                        }
                        else
                        {
                            // Two characters upper-case: combine with lower-case letters to the right
                            // "IDs"
                            allUpper = false;
                        }
                    }
                }
            }

            if (offset < pascal.Length)
            {
                yield return pascal.Substring(offset);
            }
        }

        private string GetPropertyName(string className, string original, HashSet<string> otherProperties)
        {
            string pascal = CSharpifyName(original);

            if (pascal == className)
            {
                // invalid
                var words = SplitPascal(pascal).ToArray();

                if (words.Length > 1)
                {
                    string removeFirst = string.Join(string.Empty, words.Skip(1));

                    if (!otherProperties.Contains(removeFirst))
                    {
                        return removeFirst;
                    }

                    string removeLast = string.Join(string.Empty, words.Reverse().Skip(1).Reverse());

                    if (!otherProperties.Contains(removeLast))
                    {
                        return removeLast;
                    }
                }

                string field = pascal + "Value";
                if (!otherProperties.Contains(field))
                {
                    return field;
                }

                field = "The" + pascal;
                if (!otherProperties.Contains(field))
                {
                    return field;
                }

                field = pascal + "Field";
                if (!otherProperties.Contains(field))
                {
                    return field;
                }

                field = pascal + "Property";
                if (!otherProperties.Contains(field))
                {
                    return field;
                }

                field = "Inner" + pascal;
                if (!otherProperties.Contains(field))
                {
                    return field;
                }

                int number = 1;
                while (otherProperties.Contains(pascal + number))
                {
                    number++;
                }

                return pascal + number;
            }

            return pascal;
        }

        private string GetDefaultAssignmentCode(CodeConstructor ctor, string fieldName)
        {
            CodeExpression defaultValue = ctor?.Statements.OfType<CodeAssignStatement>().Where(a => (a.Left as CodeFieldReferenceExpression)?.FieldName == fieldName).Select(a => a.Right).FirstOrDefault();

            string defaultAssignment = string.Empty;
            if (defaultValue != null)
            {
                StringBuilder defaultValueCode = new StringBuilder();
                using (var stringWriter = new StringWriter(defaultValueCode))
                {
                    new CSharpCodeProvider().GenerateCodeFromExpression(defaultValue, stringWriter, new CodeGeneratorOptions());
                }

                defaultAssignment = $" = {defaultValueCode};";
            }

            return defaultAssignment;
        }

        private void SpecifyXmlNameIfRequired(CodeTypeMember modifiedProperty, string originalName)
        {
            var elementName = modifiedProperty.CustomAttributes.FirstOrDefault(a => a.Name == "System.Xml.Serialization.XmlElementAttribute");
            var attributeName = modifiedProperty.CustomAttributes.FirstOrDefault(a => a.Name == "System.Xml.Serialization.XmlAttributeAttribute");

            if (elementName == null)
            {
                if (attributeName == null)
                {
                    modifiedProperty.CustomAttributes.Add(new CodeAttributeDeclaration("System.Xml.Serialization.XmlElementAttribute", new CodeAttributeArgument(new CodePrimitiveExpression(originalName))));
                }
                else
                {
                    var named = attributeName.Arguments.OfType<CodeAttributeArgument>().FirstOrDefault(a => string.IsNullOrEmpty(a.Name) || a.Name == "AttributeName");
                    if (named == null)
                    {
                        attributeName.Arguments.Add(new CodeAttributeArgument("AttributeName", new CodePrimitiveExpression(originalName)));
                    }
                }
            }
        }

        public void PostProcess(CodeNamespace codeNamespace)
        {
            foreach (var source in codeNamespace.Classes().ToArray())
            {
                var ctor = source.DefaultConstructor();

                if (ctor != null)
                {
                    source.Members.Remove(ctor);
                }

                var valueTypeProperties = RemoveSpecifiedProperties(source);

                var optionalValueTypeProperties = source.Properties().Where(p => valueTypeProperties.Contains(p.Name)).ToArray();
                var otherProperties = source.Properties().Where(p => !valueTypeProperties.Contains(p.Name)).ToArray();

                HashSet<string> allProperties = new HashSet<string>(
                    optionalValueTypeProperties.Select(p => CSharpifyName(p.Name))
                    .Concat(otherProperties.Select(p => CSharpifyName(p.Name))), StringComparer.Ordinal);

                string className = source.Name;

                foreach (var property in optionalValueTypeProperties)
                {
                    source.Members.Remove(property);
                    
                    string privateFieldName = RemoveAssociatedField(source, property);


                    string indentation = new string(' ', 8);
                    string accessor = "public";
                    string escapedTypeName = property.Type.GetEscapedSimplifiedTypeName();
                    string propertyName = GetPropertyName(className, property.Name, allProperties);
                    allProperties.Remove(CSharpifyName(property.Name));
                    allProperties.Add(propertyName);

                    string escapedPropertyName = CodeIdentifier.MakeValid(propertyName);


                    var replacement = new CodeSnippetTypeMember($"{indentation}{accessor} {escapedTypeName}? {escapedPropertyName} {{ get; set; }}{GetDefaultAssignmentCode(ctor, privateFieldName)}");
                    replacement.Comments.Clear();
                    replacement.Comments.AddRange(property.Comments);
                    replacement.CustomAttributes.Add(new CodeAttributeDeclaration("System.Xml.Serialization.XmlIgnoreAttribute"));
                    source.Members.Add(replacement);
                    
                    string proxyPropertyName = CodeIdentifier.MakeValid($"__{propertyName}");
                    string proxySpecifiedPropertyName = CodeIdentifier.MakeValid($"__{propertyName}Specified");

                    var proxy = new CodeMemberProperty();
                    proxy.Type = property.Type;
                    proxy.Name = proxyPropertyName;
                    proxy.Attributes = property.Attributes;
                    // return property ?? default(type);
                    proxy.GetStatements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression($"{escapedPropertyName} ?? default({escapedTypeName})")));
                    // property = value != default(type) ? value : null;
                    proxy.SetStatements.Add(new CodeSnippetExpression($"{escapedPropertyName} = value != default({escapedTypeName}) ? value : ({escapedTypeName}?)null"));

                    proxy.SetStatements.Add(new CodeAssignStatement(
                                                new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), proxySpecifiedPropertyName),
                                                new CodeBinaryOperatorExpression(new CodePropertySetValueReferenceExpression(),
                                                                                 CodeBinaryOperatorType.IdentityInequality,
                                                                                 new CodeDefaultValueExpression(property.Type))));
                    proxy.CustomAttributes.Clear();
                    proxy.CustomAttributes.AddRange(property.CustomAttributes);
                    SpecifyXmlNameIfRequired(proxy, property.Name);
                    proxy.CustomAttributes.AddOrReplace(new CodeAttributeDeclaration("System.ComponentModel.EditorBrowsableAttribute", new CodeAttributeArgument(new CodeSnippetExpression("System.ComponentModel.EditorBrowsableState.Never"))));
                    source.Members.Add(proxy);

                    //var proxySpecified = new CodeSnippetTypeMember($"{indentation}{accessor} bool {proxySpecifiedPropertyName} {{ get; set; }}");

                    var proxySpecified = new CodeMemberProperty();
                    proxySpecified.Type = new CodeTypeReference(typeof(bool));
                    proxySpecified.Name = proxySpecifiedPropertyName;
                    // return property != null;
                    proxySpecified.GetStatements.Add(new CodeMethodReturnStatement(
                                                         new CodeBinaryOperatorExpression(
                                                             new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), propertyName),
                                                             CodeBinaryOperatorType.IdentityInequality, new CodeSnippetExpression("null"))));
                    // property = value ? property : null;
                    proxySpecified.SetStatements.Add(new CodeSnippetExpression($"{escapedPropertyName} = value ? {escapedPropertyName} : null"));

                    proxySpecified.CustomAttributes.Clear();
                    proxySpecified.CustomAttributes.Add(new CodeAttributeDeclaration("System.Xml.Serialization.XmlIgnoreAttribute"));
                    proxySpecified.CustomAttributes.AddOrReplace(new CodeAttributeDeclaration("System.ComponentModel.EditorBrowsableAttribute", new CodeAttributeArgument(new CodeSnippetExpression("System.ComponentModel.EditorBrowsableState.Never"))));
                    source.Members.Add(proxySpecified);
                }

                foreach (var property in otherProperties)
                {
                    source.Members.Remove(property);

                    string privateFieldName = RemoveAssociatedField(source, property);

                    string indentation = new string(' ', 8);
                    string accessor = "public";
                    string escapedTypeName = property.Type.GetEscapedSimplifiedTypeName();
                    string propertyName = GetPropertyName(className, property.Name, allProperties);
                    allProperties.Remove(CSharpifyName(property.Name));
                    allProperties.Add(propertyName);
                    string escapedPropertyName = CodeIdentifier.MakeValid(propertyName);

                    bool propertyNameChanged = !propertyName.Equals(property.Name, StringComparison.Ordinal);

                    // array types getting messed up too!


                    var replacement = new CodeSnippetTypeMember($"{indentation}{accessor} {escapedTypeName} {escapedPropertyName} {{ get; set; }}{GetDefaultAssignmentCode(ctor, privateFieldName)}");
                    replacement.Attributes = property.Attributes;
                    replacement.CustomAttributes.Clear();
                    replacement.CustomAttributes.AddRange(property.CustomAttributes);
                    if (propertyNameChanged)
                    {
                        SpecifyXmlNameIfRequired(replacement, property.Name);
                    }
                    
                    StringBuilder generatedAttributes = new StringBuilder();
                    foreach (CodeAttributeDeclaration declaration in replacement.CustomAttributes)
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

                    generatedAttributes.AppendLine(replacement.Text);

                    replacement.Text = generatedAttributes.ToString();

                    replacement.Comments.Clear();
                    replacement.Comments.AddRange(property.Comments);
                    
                    source.Members.Add(replacement);
                }
            }
        }



        private HashSet<string> RemoveSpecifiedProperties(CodeTypeDeclaration source)
        {
            HashSet<string> valueTypeProperties = new HashSet<string>();

            foreach (var property in source.Properties().Where(p => p.IsSpecifiedProperty()).ToArray())
            {
                valueTypeProperties.Add(property.SpecifiedPropertyName());
                source.Members.Remove(property);
                RemoveAssociatedField(source, property);
            }

            return valueTypeProperties;
        }

        // returns field name
        private string RemoveAssociatedField(CodeTypeDeclaration source, CodeMemberProperty property)
        {
            var assignTarget = property.SetStatements.OfType<CodeAssignStatement>().FirstOrDefault()?.Left as CodeFieldReferenceExpression;

            if (assignTarget != null)
            {
                var field = source.Fields().FirstOrDefault(f => f.Name == assignTarget.FieldName);
                if (field != null)
                {
                    source.Members.Remove(field);
                    return field.Name;
                }
            }

            return null;
        }

        public string SchemaNamespace { get; set; }
    }
}
