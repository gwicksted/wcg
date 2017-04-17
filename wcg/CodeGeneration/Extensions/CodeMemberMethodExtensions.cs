using System.CodeDom;
using System.Linq;

namespace wcg.CodeGeneration.Extensions
{
    internal static class CodeMemberMethodExtensions
    {
        public static bool IsBeginNewAsync(this CodeMemberMethod method)
        {
            return IsPublic(method) && method.ReturnType.IsVoid() && method.Name.EndsWith("Async");
        }

        public static bool IsBeginAsync(this CodeMemberMethod method)
        {
            return IsPublic(method) && method.ReturnType.IsIAsyncResult();
        }

        public static bool IsEndAsync(this CodeMemberMethod method)
        {
            return IsPublic(method) && method.Parameters.OfType<CodeParameterDeclarationExpression>().Any(p => p.Type.IsIAsyncResult());
        }

        public static bool IsPrivate(this CodeMemberMethod method)
        {
            return (method.Attributes & MemberAttributes.Private) == MemberAttributes.Private;
        }

        public static bool IsPublic(this CodeMemberMethod method)
        {
            return (method.Attributes & MemberAttributes.Public) == MemberAttributes.Public;
        }

        public static bool IsAsyncCallback(this CodeMemberMethod method)
        {
            return IsPrivate(method) && method.Name.StartsWith("On") && method.Name.EndsWith("OperationCompleted");
        }

        public static bool IsSynchronous(this CodeMemberMethod method)
        {
            return IsPublic(method) && !IsBeginAsync(method) && !IsBeginNewAsync(method) && !IsEndAsync(method) && !IsAsyncCallback(method);
        }

        public static void ToPrivate(this CodeMemberMethod method)
        {
            method.Attributes = method.Attributes.ToPrivate();
        }

        public static string NormalizeName(this CodeMemberMethod method)
        {
            string name = method.Name;

            if (name.StartsWith("On") && name.EndsWith("OperationCompleted"))
            {
                return name.Substring(2, name.Length - 20);
            }

            if (name.EndsWith("Async"))
            {
                return name.Substring(0, name.Length - 5);
            }

            if (name.StartsWith("Begin"))
            {
                return name.Substring(5);
            }

            if (name.StartsWith("End"))
            {
                return name.Substring(3);
            }

            return name;
        }

        public static CodeMethodReferenceExpression ToThisCallReference(this CodeMemberMethod method)
        {
            return new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), method.Name);
        }
    }
}
