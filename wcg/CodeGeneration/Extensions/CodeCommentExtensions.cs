using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace wcg.CodeGeneration.Extensions
{
    internal static class CodeCommentExtensions
    {
        public static bool IsNullOrEmpty(this CodeCommentStatement comment)
        {
            return comment?.Comment?.IsNullOrEmpty() ?? true;
        }

        public static bool IsNullOrEmpty(this CodeComment comment)
        {
            return string.IsNullOrWhiteSpace(comment?.Text) || (comment.DocComment && comment.Text.Trim() == "<remarks/>");
        }

        public static IEnumerable<CodeCommentStatement> Comments(this CodeCommentStatementCollection comments)
        {
            return comments?.OfType<CodeCommentStatement>() ?? new CodeCommentStatement[0];
        }

        public static IEnumerable<CodeCommentStatement> BlankComments(this CodeCommentStatementCollection comments)
        {
            return comments?.Comments().Where(IsNullOrEmpty);
        }
    }
}
