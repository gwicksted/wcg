using System.Reflection;

namespace wcg.CodeGeneration.Extensions
{
    internal static class TypeAttributesExtensions
    {
        public static TypeAttributes ToPrivate(this TypeAttributes attributes)
        {
            return attributes & (~(TypeAttributes.Public | TypeAttributes.NestedAssembly | TypeAttributes.NestedFamANDAssem | TypeAttributes.NestedFamORAssem | TypeAttributes.NestedFamily | TypeAttributes.NestedPublic)) | TypeAttributes.NotPublic | TypeAttributes.NestedPrivate;
        }
    }
}
