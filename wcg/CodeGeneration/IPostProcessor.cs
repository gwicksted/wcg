using System.CodeDom;

namespace wcg.CodeGeneration
{
    internal interface IPostProcessor
    {
        void PostProcess(CodeNamespace codeNamespace);

        string SchemaNamespace { get; set; }
    }
}
