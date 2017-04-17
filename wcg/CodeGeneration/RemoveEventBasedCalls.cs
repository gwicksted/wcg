using System.CodeDom;
using System.Linq;
using wcg.CodeGeneration.Extensions;

namespace wcg.CodeGeneration
{
    internal class RemoveEventBasedCalls : IPostProcessor
    {
        public void PostProcess(CodeNamespace codeNamespace)
        {
            foreach (var source in codeNamespace.Classes().ToArray())
            {
                if (source.IsCompletedEventArgsDeclaration())
                {
                    codeNamespace.Types.Remove(source);
                }
                else
                {
                    foreach (var ev in source.Events().Where(e => e.IsCompletedEvent()).ToArray())
                    {
                        source.Members.Remove(ev);
                    }

                    foreach (var callback in source.Fields().Where(f => f.Type.IsSendOrPostCallback()).ToArray())
                    {
                        source.Members.Remove(callback);
                    }
                }
            }

            foreach (var del in codeNamespace.Delegates().Where(d => d.IsEventHandlerDelegate()).ToArray())
            {
                codeNamespace.Types.Remove(del);
            }
        }

        public string SchemaNamespace { get; set; }
    }
}
