using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

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
    }
}
