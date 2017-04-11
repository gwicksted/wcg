using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wcg.CodeGeneration.Extensions;

namespace wcg.CodeGeneration
{
    internal class GenerateTaskApiMethods : IPostProcessor
    {
        private static CodeTypeReference TaskType => new CodeTypeReference(typeof(Task));

        private static CodeMethodReferenceExpression FromAsyncReference => new CodeMethodReferenceExpression(new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(TaskType), "Factory"), "FromAsync");

        private static readonly CodeSnippetExpression NullExpression = new CodeSnippetExpression("null");

        private class RelatedMethods
        {
            public CodeMemberMethod Begin { get; set; }
            public CodeMemberMethod End { get; set; }
            public CodeMemberMethod Sync { get; set; }

            public bool HaveAll => Begin != null && End != null && Sync != null;
        }
        
        private RelatedMethods FindOrAddRelated(CodeMemberMethod method, IDictionary<string, RelatedMethods> related)
        {
            string normalized = method.NormalizeName();

            RelatedMethods all;

            if (!related.TryGetValue(normalized, out all))
            {
                all = new RelatedMethods();
                related.Add(normalized, all);
            }

            return all;
        }

        private IDictionary<string, RelatedMethods> ComposeRelations(CodeTypeDeclaration cls)
        {
            IDictionary<string, RelatedMethods> related = new Dictionary<string, RelatedMethods>();

            foreach (var member in cls.Methods().ToArray())
            {
                if (member.IsBeginNewAsync())
                {
                    cls.Members.Remove(member);
                }
                else if (member.IsAsyncCallback())
                {
                    cls.Members.Remove(member);
                }
                else if (member.IsBeginAsync())
                {
                    FindOrAddRelated(member, related).Begin = member;
                }
                else if (member.IsEndAsync())
                {
                    FindOrAddRelated(member, related).End = member;
                }
                else if (member.IsSynchronous())
                {
                    FindOrAddRelated(member, related).Sync = member;
                }
            }

            return related;
        }


        public void PostProcess(CodeNamespace codeNamespace)
        {
            foreach (var source in codeNamespace.Classes().ToArray())
            {
                var related = ComposeRelations(source);

                foreach (var similar in related.Where(s => s.Value.HaveAll))
                {
                    var begin = similar.Value.Begin;
                    var end = similar.Value.End;
                    var sync = similar.Value.Sync;

                    begin.ToPrivate();
                    end.ToPrivate();

                    var asynchronous = new CodeMemberMethod
                    {
                        Name = similar.Key + "Async",
                        ReturnType = TaskType,
                        Attributes = MemberAttributes.Public | MemberAttributes.Final
                    };
                    
                    asynchronous.ReturnType.TypeArguments.Add(sync.ReturnType); // Task<T>

                    var request = sync.Parameters.OfType<CodeParameterDeclarationExpression>().First();

                    asynchronous.Parameters.Add(request);
                    
                    // Task.Factory.FromAsync(begin, end, request, null);
                    var fromAsyncExpression = new CodeMethodInvokeExpression(FromAsyncReference, begin.ToThisCallReference(), end.ToThisCallReference(), request.ToArgumentReference(), NullExpression);
                    
                    asynchronous.Statements.Add(new CodeMethodReturnStatement(fromAsyncExpression));

                    source.Members.Add(asynchronous);
                }
            }
        }

        public string SchemaNamespace { get; set; }
    }
}