using System.CodeDom;

namespace wcg.CodeGeneration.Extensions
{
    internal static class MemberAttributesExtensions
    {
        public static MemberAttributes ToPrivate(this MemberAttributes attributes)
        {
            return attributes & (~(MemberAttributes.Public | MemberAttributes.Family | MemberAttributes.FamilyAndAssembly | MemberAttributes.Assembly | MemberAttributes.FamilyOrAssembly)) | MemberAttributes.Private;
        }
    }
}
