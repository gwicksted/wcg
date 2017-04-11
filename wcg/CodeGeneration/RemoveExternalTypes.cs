using System.CodeDom;
using System.Linq;
using wcg.CodeGeneration.Extensions;

namespace wcg.CodeGeneration
{
    internal class RemoveExternalTypes : IPostProcessor
    {
        public string SchemaNamespace { get; set; }

        public void PostProcess(CodeNamespace codeNamespace)
        {
            var sources = codeNamespace.Types.OfType<CodeTypeDeclaration>().ToArray();

            foreach (var source in sources)
            {
                var keep = ShouldDefine(source);

                DescribeAction(source, keep);

                if (!keep)
                {
                    codeNamespace.Types.Remove(source);
                }
            }
        }
        
        private void DescribeAction(CodeTypeDeclaration source, bool keep)
        {
            if (keep)
            {
                Output.Added(source.Name, source.GetXmlNamespace());
            }
            else
            {
                Output.Removed(source.Name, source.GetXmlNamespace());
            }
        }

        private bool ShouldDefine(CodeTypeDeclaration declaration)
        {
            var ns = declaration.GetXmlNamespace();

            return string.IsNullOrEmpty(ns) || ns == SchemaNamespace;
        }
    }
}
