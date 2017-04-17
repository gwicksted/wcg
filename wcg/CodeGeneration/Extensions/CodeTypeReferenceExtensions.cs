using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace wcg.CodeGeneration.Extensions
{
    internal static class CodeTypeReferenceExtensions
    {
        public static bool IsBoolean(this CodeTypeReference reference)
        {
            return reference.BaseType == "System.Boolean";
        }
        
        public static bool IsVoid(this CodeTypeReference reference)
        {
            return reference.BaseType == "System.Void";
        }

        public static bool IsIAsyncResult(this CodeTypeReference reference)
        {
            return reference.BaseType == "System.IAsyncResult";
        }

        public static bool IsSendOrPostCallback(this CodeTypeReference reference)
        {
            return reference.BaseType == "System.Threading.SendOrPostCallback";
        }

        public static bool IsArray(this CodeTypeReference reference)
        {
            return reference.ArrayElementType != null;
        }

        public static string GetArraySpecification(this CodeTypeReference reference)
        {
            var specification = string.Empty;

            for (int i = 0; i < reference.ArrayRank; i++)
            {
                specification += "[]";
            }

            return specification;
        }

        public static bool HasGenericArgs(this CodeTypeReference reference)
        {
            return reference.TypeArguments.Count > 0;
        }

        public static IEnumerable<CodeTypeReference> GetGenericArgs(this CodeTypeReference reference)
        {
            return reference.TypeArguments.OfType<CodeTypeReference>();
        }

        public static string GetGenericSpecification(this CodeTypeReference reference)
        {
            if (reference.HasGenericArgs())
            {
                return "<" + string.Join(", ", reference.GetGenericArgs().Select(arg => arg.GetFullTypeName())) + ">";
            }

            return null;
        }

        public static string GetSimplifiedGenericSpecification(this CodeTypeReference reference)
        {
            if (reference.HasGenericArgs())
            {
                return "<" + string.Join(", ", reference.GetGenericArgs().Select(arg => arg.GetSimplifiedTypeName())) + ">";
            }

            return null;
        }

        public static string GetEscapedSimplifiedGenericSpecification(this CodeTypeReference reference)
        {
            if (reference.HasGenericArgs())
            {
                return "<" + string.Join(", ", reference.GetGenericArgs().Select(arg => arg.GetEscapedSimplifiedTypeName())) + ">";
            }

            return null;
        }

        private static string ToSimplifiedType(this string type)
        {
            switch (type)
            {
                case "System.Void": return "void";
                case "System.String": return "string";
                case "System.Boolean": return "bool";
                case "System.Char": return "char";
                case "System.Byte": return "byte";
                case "System.SByte": return "sbyte";
                case "System.Int16": return "short";
                case "System.UInt16": return "ushort";
                case "System.Int32": return "int";
                case "System.UInt32": return "uint";
                case "System.Int64": return "long";
                case "System.UInt64": return "ulong";
                case "System.Double": return "double";
                case "System.Single": return "float";
                case "System.Decimal": return "decimal";
                default: return type;
            }
        }

        public static string EscapeNamespace(this string type)
        {
            return string.Join(".", type.Split('.').Select(CodeIdentifier.MakeValid));
        }

        public static string GetSimplifiedTypeName(this CodeTypeReference reference)
        {
            string typeName = reference.BaseType.ToSimplifiedType();

            if (reference.HasGenericArgs())
            {
                typeName += reference.GetSimplifiedGenericSpecification();
            }

            if (reference.IsArray())
            {
                typeName += reference.GetArraySpecification();
            }

            return typeName;
        }

        public static string GetEscapedSimplifiedTypeName(this CodeTypeReference reference)
        {
            string typeName = reference.BaseType.ToSimplifiedType().EscapeNamespace();
            
            if (reference.HasGenericArgs())
            {
                typeName += reference.GetEscapedSimplifiedGenericSpecification();
            }

            if (reference.IsArray())
            {
                typeName += reference.GetArraySpecification();
            }

            return typeName;
        }

        public static string GetFullTypeName(this CodeTypeReference reference)
        {
            string typeName = reference.BaseType;

            if (reference.HasGenericArgs())
            {
                typeName += reference.GetGenericSpecification();
            }

            if (reference.IsArray())
            {
                typeName += reference.GetArraySpecification();
            }

            return typeName;
        }
    }
}
