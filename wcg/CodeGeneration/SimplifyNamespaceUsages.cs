using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using wcg.CodeGeneration.Extensions;

namespace wcg.CodeGeneration
{
    internal class SimplifyNamespaceUsages : IPostProcessor
    {
        private readonly HashSet<string> _imports = new HashSet<string>();
        
        public void PostProcess(CodeNamespace codeNamespace)
        {
            foreach (var source in codeNamespace.Classes().ToArray())
            {
                SimplifyAttributes(source.CustomAttributes);

                SimplifyTypes(source.BaseTypes);

                foreach (var field in source.Fields())
                {
                    SimplifyType(field.Type);
                    SimplifyAttributes(field.CustomAttributes);
                    SimplifyExpression(field.InitExpression);
                }

                foreach (var snippet in source.Snippets())
                {
                    if (snippet.Text.Contains("{ get; set; }"))
                    {
                        string original = snippet.Text;
                        string originalAttributes = snippet.CustomAttributes.GenerateSourceCode();

                        if (snippet.CustomAttributes != null)
                        {
                            SimplifyAttributes(snippet.CustomAttributes);

                            string output = original;

                            string replacementAttributes = snippet.CustomAttributes.GenerateSourceCode();

                            if (!string.IsNullOrEmpty(originalAttributes))
                            {
                                output = output.Replace(originalAttributes, replacementAttributes);
                            }

                            snippet.Text = output;
                        }
                    }
                }

                foreach (var property in source.Properties())
                {
                    SimplifyType(property.Type);
                    SimplifyAttributes(property.CustomAttributes);
                    
                    if (property.HasGet)
                    {
                        SimplifyStatements(property.GetStatements);
                    }

                    if (property.HasSet)
                    {
                        SimplifyStatements(property.SetStatements);
                    }

                    SimplifyTypes(property.ImplementationTypes);
                }
                
                SimplifyTypeParameters(source.TypeParameters);
                
                foreach (var member in source.Methods())
                {
                    SimplifyType(member.ReturnType);
                    SimplifyType(member.PrivateImplementationType);

                    SimplifyTypeParameters(member.TypeParameters);
                    
                    SimplifyAttributes(member.CustomAttributes);
                    SimplifyAttributes(member.ReturnTypeCustomAttributes);

                    SimplifyParameters(member.Parameters);

                    SimplifyTypes(member.ImplementationTypes);
                    SimplifyStatements(member.Statements);
                }
            }

            codeNamespace.Imports.AddRange(_imports.Select(i => new CodeNamespaceImport(i)).ToArray());
        }

        private void SimplifyParameters(CodeParameterDeclarationExpressionCollection parameters)
        {
            foreach (var parameter in parameters.OfType<CodeParameterDeclarationExpression>())
            {
                SimplifyType(parameter.Type);
                SimplifyAttributes(parameter.CustomAttributes);
            }
        }

        private void SimplifyTypeParameters(CodeTypeParameterCollection parameters)
        {
            foreach (var parameter in parameters.OfType<CodeTypeParameter>())
            {
                SimplifyTypes(parameter.Constraints);
                SimplifyAttributes(parameter.CustomAttributes);
            }
        }

        private void SimplifyTypes(CodeTypeReferenceCollection types)
        {
            foreach (var type in types.OfType<CodeTypeReference>())
            {
                SimplifyType(type);
            }
        }

        private void SimplifyAttributes(CodeAttributeDeclarationCollection attributes)
        {
            foreach (var instance in attributes.OfType<CodeAttributeDeclaration>())
            {
                SimplifyAttribute(instance);
            }
        }

        private void SimplifyStatement(CodeStatement statement)
        {
            var variable = statement as CodeVariableDeclarationStatement;
            if (variable != null)
            {
                SimplifyType(variable.Type);
                SimplifyExpression(variable.InitExpression);
            }

            var ret = statement as CodeMethodReturnStatement;
            if (ret != null)
            {
                SimplifyExpression(ret.Expression);
            }

            var assign = statement as CodeAssignStatement;
            if (assign != null)
            {
                SimplifyExpression(assign.Left);
                SimplifyExpression(assign.Right);
            }

            var attachEvent = statement as CodeAttachEventStatement;
            if (attachEvent != null)
            {
                SimplifyExpression(attachEvent.Event.TargetObject);
                SimplifyExpression(attachEvent.Listener);
            }

            var removeEvent = statement as CodeRemoveEventStatement;
            if (removeEvent != null)
            {
                SimplifyExpression(removeEvent.Event.TargetObject);
                SimplifyExpression(removeEvent.Listener);
            }

            var conditional = statement as CodeConditionStatement;
            if (conditional != null)
            {
                SimplifyExpression(conditional.Condition);
                SimplifyStatements(conditional.TrueStatements);
                SimplifyStatements(conditional.FalseStatements);
            }

            var expression = statement as CodeExpressionStatement;
            if (expression != null)
            {
                SimplifyExpression(expression.Expression);
            }

            var iteration = statement as CodeIterationStatement;
            if (iteration != null)
            {
                SimplifyStatement(iteration.IncrementStatement);
                SimplifyStatement(iteration.InitStatement);
                SimplifyExpression(iteration.TestExpression);
                SimplifyStatements(iteration.Statements);
            }

            var throwException = statement as CodeThrowExceptionStatement;
            if (throwException != null)
            {
                SimplifyExpression(throwException.ToThrow);
            }

            var tryStatement = statement as CodeTryCatchFinallyStatement;
            if (tryStatement != null)
            {
                SimplifyStatements(tryStatement.TryStatements);
                SimplifyCatchClauses(tryStatement.CatchClauses);
                SimplifyStatements(tryStatement.FinallyStatements);
            }
        }

        private void SimplifyCatchClauses(CodeCatchClauseCollection catches)
        {
            foreach (var catcher in catches.OfType<CodeCatchClause>())
            {
                SimplifyType(catcher.CatchExceptionType);
                SimplifyStatements(catcher.Statements);
            }
        }

        private void SimplifyStatements(CodeStatementCollection statements)
        {
            foreach (var statement in statements.OfType<CodeStatement>())
            {
                SimplifyStatement(statement);
            }
        }

        private void SimplifyExpressions(CodeExpressionCollection expressions)
        {
            foreach (var parameter in expressions.OfType<CodeExpression>())
            {
                SimplifyExpression(parameter);
            }
        }

        private void SimplifyExpression(CodeExpression expression)
        {
            var create = expression as CodeObjectCreateExpression;
            if (create != null)
            {
                SimplifyType(create.CreateType);
                SimplifyExpressions(create.Parameters);
            }

            var arr = expression as CodeArrayCreateExpression;
            if (arr != null)
            {
                SimplifyType(arr.CreateType);
                SimplifyExpressions(arr.Initializers);
                SimplifyExpression(arr.SizeExpression);
            }

            var arrInd = expression as CodeArrayIndexerExpression;
            if (arrInd != null)
            {
                SimplifyExpressions(arrInd.Indices);
                SimplifyExpression(arrInd.TargetObject);
            }

            var binop = expression as CodeBinaryOperatorExpression;
            if (binop != null)
            {
                SimplifyExpression(binop.Left);
                SimplifyExpression(binop.Right);
            }

            var cast = expression as CodeCastExpression;
            if (cast != null)
            {
                SimplifyExpression(cast.Expression);
                SimplifyType(cast.TargetType);
            }

            var def = expression as CodeDefaultValueExpression;
            if (def != null)
            {
                SimplifyType(def.Type);
            }

            var delegateCreate = expression as CodeDelegateCreateExpression;
            if (delegateCreate != null)
            {
                SimplifyType(delegateCreate.DelegateType);
                SimplifyExpression(delegateCreate.TargetObject);
            }

            var delegateInvoke = expression as CodeDelegateInvokeExpression;
            if (delegateInvoke != null)
            {
                SimplifyExpression(delegateInvoke.TargetObject);
                SimplifyExpressions(delegateInvoke.Parameters);
            }

            var direction = expression as CodeDirectionExpression;
            if (direction != null)
            {
                SimplifyExpression(direction.Expression);
            }

            var evRef = expression as CodeEventReferenceExpression;
            if (evRef != null)
            {
                SimplifyExpression(evRef.TargetObject);
            }

            var fieldRef = expression as CodeFieldReferenceExpression;
            if (fieldRef != null)
            {
                ApplySimplification(fieldRef, f => f.FieldName, (f, n) => f.FieldName = n);
                SimplifyExpression(fieldRef.TargetObject);
            }

            var indexer = expression as CodeIndexerExpression;
            if (indexer != null)
            {
                SimplifyExpression(indexer.TargetObject);
                SimplifyExpressions(indexer.Indices);
            }

            var call = expression as CodeMethodInvokeExpression;
            if (call != null)
            {
                SimplifyMethodReference(call.Method);
                SimplifyExpressions(call.Parameters);
            }

            var methodRef = expression as CodeMethodReferenceExpression;
            if (methodRef != null)
            {
                SimplifyMethodReference(methodRef);
            }

            var parameterDecl = expression as CodeParameterDeclarationExpression;
            if (parameterDecl != null)
            {
                SimplifyType(parameterDecl.Type);
                SimplifyAttributes(parameterDecl.CustomAttributes);
            }

            var propRef = expression as CodePropertyReferenceExpression;
            if (propRef != null)
            {
                ApplySimplification(propRef, p => p.PropertyName, (p, n) => p.PropertyName = n);
                SimplifyExpression(propRef.TargetObject);
            }

            var getType = expression as CodeTypeOfExpression;
            if (getType != null)
            {
                SimplifyType(getType.Type);
            }

            var typeRef = expression as CodeTypeReferenceExpression;
            if (typeRef != null)
            {
                SimplifyType(typeRef.Type);
            }
        }

        private void SimplifyMethodReference(CodeMethodReferenceExpression method)
        {
            ApplySimplification(method, m => m.MethodName, (m, n) => m.MethodName = n);

            SimplifyTypes(method.TypeArguments);

            SimplifyExpression(method.TargetObject);
        }

        private void SimplifyAttribute(CodeAttributeDeclaration instance)
        {
            ApplySimplification(instance, i => i.Name, (i, n) => i.Name = n);

            if (instance.Name.EndsWith("Attribute", StringComparison.Ordinal))
            {
                instance.Name = instance.Name.Substring(0, instance.Name.Length - 9);
            }
            
            SimplifyType(instance.AttributeType);
            SimplifyArguments(instance.Arguments);
        }

        private void SimplifyArguments(CodeAttributeArgumentCollection args)
        {
            foreach (var arg in args.OfType<CodeAttributeArgument>())
            {
                SimplifyArgument(arg);
            }
        }

        private void SimplifyArgument(CodeAttributeArgument arg)
        {
            ApplySimplification(arg, a => a.Name, (a, n) => a.Name = n);

            SimplifyExpression(arg.Value);
        }

        private void SimplifyType(CodeTypeReference type)
        {
            if (type == null)
            {
                return;
            }
            
            ApplySimplification(type, t => t.BaseType, (t, n) => t.BaseType = n);

            if (type.TypeArguments != null)
            {
                SimplifyTypes(type.TypeArguments);
            }
        }

        private void ApplySimplification<T>(T obj, Func<T, string> getTypeName, Action<T, string> nameChange)
        {
            if (ReferenceEquals(obj, null))
            {
                return;
            }

            string fullName = getTypeName.Invoke(obj);
            var split = SplitNamespace(fullName);

            string ns = split.Item1;
            string name = split.Item2;

            if (!string.IsNullOrEmpty(ns))
            {
                _imports.Add(ns);
                nameChange(obj, name);
            }
        }

        public string SchemaNamespace { get; set; }

        private Tuple<string, string> SplitNamespace(string name)
        {
            var pieces = name.Split('.');

            if (pieces.Length > 1)
            {
                return new Tuple<string, string>(string.Join(".", pieces.AllButLast()), pieces.Last());
            }

            return new Tuple<string, string>(null, name);
        }
    }
}
