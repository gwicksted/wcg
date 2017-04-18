using System.CodeDom;

namespace wcg.CodeGeneration
{
    internal class CodePostProcessorFactory : IPostProcessor
    {
        public void PostProcess(CodeNamespace codeNamespace)
        {
            new RemoveImports().PostProcess(codeNamespace);
            var ext = new RemoveExternalTypes();
            ext.SchemaNamespace = SchemaNamespace;
            ext.PostProcess(codeNamespace);
            new RemoveEventBasedCalls().PostProcess(codeNamespace);
            new GenerateTaskApiMethods().PostProcess(codeNamespace);
            new ShorthandProperties().PostProcess(codeNamespace);
            new SimplifyNamespaceUsages().PostProcess(codeNamespace);
            new RemoveEmptyRemarksComments().PostProcess(codeNamespace);
            new AddComments().PostProcess(codeNamespace);
        }

        public string SchemaNamespace { get; set; }
    }
}
