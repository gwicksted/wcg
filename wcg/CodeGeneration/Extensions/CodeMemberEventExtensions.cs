using System.CodeDom;

namespace wcg.CodeGeneration.Extensions
{
    internal static class CodeMemberEventExtensions
    {
        public static void ToPrivate(this CodeMemberEvent ev)
        {
            ev.Attributes = ev.Attributes.ToPrivate();
        }

        public static bool IsCompletedEvent(this CodeMemberEvent ev)
        {
            return ev.Name.EndsWith("Completed") && ev.Type.BaseType.EndsWith("CompletedEventHandler");
        }
    }
}
