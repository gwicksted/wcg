using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace wcg.CodeGeneration.Extensions
{
    internal static class CodeTypeDeclarationExtensions
    {

        private static CodeAttributeDeclaration FindCodeAttribute(this CodeTypeDeclaration declaration, string name)
        {
            return declaration?.CustomAttributes.OfType<CodeAttributeDeclaration>().FirstOrDefault(a => a.Name == name);
        }

        private static CodeAttributeArgument FindAttributeArgument(this CodeAttributeDeclaration declaration, string name)
        {
            return declaration?.Arguments.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name == name);
        }

        private static string ReadAttributeValue(this CodeAttributeArgument argument)
        {
            return (argument?.Value as CodePrimitiveExpression)?.Value as string;
        }

        public static string GetXmlNamespace(this CodeTypeDeclaration declaration)
        {
            return declaration.FindCodeAttribute("System.Xml.Serialization.XmlTypeAttribute").FindAttributeArgument("Namespace").ReadAttributeValue();
        }

        public static bool Extends(this CodeTypeDeclaration declaration, string baseClass)
        {
            return declaration.BaseTypes.OfType<CodeTypeReference>().Any(bt => bt.BaseType == baseClass);
        }

        public static bool IsCompletedEventArgsDeclaration(this CodeTypeDeclaration declaration)
        {
            return declaration.Name.EndsWith("CompletedEventArgs") && declaration.Extends("System.ComponentModel.AsyncCompletedEventArgs");
        }
        
        public static void ToPrivate(this CodeTypeDeclaration decl)
        {
            decl.Attributes = decl.Attributes.ToPrivate();
            decl.TypeAttributes = decl.TypeAttributes.ToPrivate();
        }

        public static IEnumerable<CodeMemberMethod> Methods(this CodeTypeDeclaration decl)
        {
            return decl.Members.OfType<CodeMemberMethod>();
        }

        public static IEnumerable<CodeMemberEvent> Events(this CodeTypeDeclaration decl)
        {
            return decl.Members.OfType<CodeMemberEvent>();
        }

        public static IEnumerable<CodeMemberField> Fields(this CodeTypeDeclaration decl)
        {
            return decl.Members.OfType<CodeMemberField>();
        }

        public static IEnumerable<CodeMemberProperty> Properties(this CodeTypeDeclaration decl)
        {
            return decl.Members.OfType<CodeMemberProperty>();
        }

        public static CodeConstructor DefaultConstructor(this CodeTypeDeclaration decl)
        {
            return decl.Members.OfType<CodeConstructor>().FirstOrDefault(c => c.Parameters.Count == 0) ?? decl.Members.OfType<CodeConstructor>().FirstOrDefault();
        }
    }
}
