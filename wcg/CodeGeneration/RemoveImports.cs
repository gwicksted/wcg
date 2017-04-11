using System.CodeDom;

namespace wcg.CodeGeneration
{
    internal class RemoveImports : IPostProcessor
    {
        public void PostProcess(CodeNamespace codeNamespace)
        {
            codeNamespace.Imports.Clear();
        }

        public string SchemaNamespace { get; set; }
    }
}
