using System.CodeDom;
using System.Linq;
using wcg.CodeGeneration.Extensions;

namespace wcg.CodeGeneration
{
    internal class RemoveEmptyRemarksComments : IPostProcessor
    {
        public void PostProcess(CodeNamespace codeNamespace)
        {
            foreach (var source in codeNamespace.Types.OfType<CodeTypeDeclaration>())
            {
                foreach (var blank in source.Comments.BlankComments().ToArray())
                {
                    source.Comments.Remove(blank);
                }

                foreach (var member in source.Members.OfType<CodeTypeMember>())
                {
                    foreach (var blank in member.Comments.BlankComments().ToArray())
                    {
                        member.Comments.Remove(blank);
                    }
                }
            }
        }

        public string SchemaNamespace { get; set; }
    }
}
