using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace wcg.CodeGeneration.Extensions
{
    internal static class CodeNamespaceExtensions
    {
        public static IEnumerable<CodeTypeDeclaration> Classes(this CodeNamespace ns)
        {
            return ns.Types.OfType<CodeTypeDeclaration>().Where(c => c.IsClass);
        }

        public static IEnumerable<CodeTypeDelegate> Delegates(this CodeNamespace ns)
        {
            return ns.Types.OfType<CodeTypeDelegate>();
        }
    }
}
