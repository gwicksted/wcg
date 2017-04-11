using System.CodeDom;

namespace wcg.CodeGeneration.Extensions
{
    internal static class CodeTypeDelegateExtensions
    {
        public static bool IsEventHandlerDelegate(this CodeTypeDelegate del)
        {
            return del.Name.EndsWith("CompletedEventHandler");
        }
    }
}
