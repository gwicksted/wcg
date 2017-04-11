using System.CodeDom;

namespace wcg.CodeGeneration.Extensions
{
    internal static class CodeParameterDeclarationExpressionExtensions
    {
        public static CodeArgumentReferenceExpression ToArgumentReference(this CodeParameterDeclarationExpression parameter)
        {
            return new CodeArgumentReferenceExpression(parameter.Name);
        }
    }
}
