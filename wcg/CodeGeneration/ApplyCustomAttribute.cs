using System;
using System.CodeDom;
using System.Linq;
using wcg.CodeGeneration.Extensions;

namespace wcg.CodeGeneration
{
    internal class ApplyCustomAttribute : IPostProcessor
    {
        private readonly string _attributeName;
        private readonly bool _applyToAsyncRequestMethods;
        private readonly bool _applyToAsyncResponseMethods;
        private readonly bool _applyToSyncMethods;

        public ApplyCustomAttribute(string attributeName, bool asyncReq, bool asyncRes, bool syncReqRes)
        {
            if (string.IsNullOrEmpty(attributeName))
            {
                throw new ArgumentNullException(nameof(attributeName));
            }

            _attributeName = attributeName;
            _applyToAsyncRequestMethods = asyncReq;
            _applyToAsyncResponseMethods = asyncRes;
            _applyToSyncMethods = syncReqRes;
        }

        private bool NotAlreadyPresent(CodeMemberMethod method)
        {
            return method.CustomAttributes.OfType<CodeAttributeDeclaration>().All(c => c.Name != _attributeName);
        }

        private bool IsApplicable(CodeMemberMethod method)
        {
            return NotAlreadyPresent(method)
                && (_applyToAsyncRequestMethods && method.IsBeginAsync() 
                || _applyToAsyncResponseMethods && method.IsEndAsync() 
                || _applyToSyncMethods && method.IsSynchronous());
        }

        public void PostProcess(CodeNamespace codeNamespace)
        {
            foreach (var source in codeNamespace.Classes().ToArray())
            {
                foreach (var member in source.Methods().Where(IsApplicable))
                {
                    member.CustomAttributes.Add(new CodeAttributeDeclaration(_attributeName));
                }
            }
        }

        public string SchemaNamespace { get; set; }
    }
}
