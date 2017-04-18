using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
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

                foreach (var snippet in source.Members.OfType<CodeSnippetTypeMember>())
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

                        // public type name

                        //"System.Xml.Serialization.XmlElementAttribute"
                        //"System.Xml.Serialization.XmlAttributeAttribute"
                        //"System.Xml.Serialization.XmlIgnoreAttribute"
                        //"System.ComponentModel.EditorBrowsableAttribute"
                    }
                }

                foreach (var property in source.Properties())
                {
                    SimplifyType(property.Type);
                    SimplifyAttributes(property.CustomAttributes);
                    
                    if (property.HasGet)
                    {
                        foreach (var statement in property.GetStatements.OfType<CodeStatement>())
                        {
                            SimplifyStatement(statement);
                        }
                    }

                    if (property.HasSet)
                    {
                        foreach (var statement in property.SetStatements.OfType<CodeStatement>())
                        {
                            SimplifyStatement(statement);
                        }
                    }

                    SimplifyTypes(property.ImplementationTypes);
                }

                foreach (var typeParam in source.TypeParameters.OfType<CodeTypeParameter>())
                {
                    SimplifyTypes(typeParam.Constraints);

                    SimplifyAttributes(typeParam.CustomAttributes);
                }

                foreach (var member in source.Methods())
                {
                    SimplifyType(member.ReturnType);
                    SimplifyType(member.PrivateImplementationType);

                    foreach (var typeParam in member.TypeParameters.OfType<CodeTypeParameter>())
                    {
                        SimplifyTypes(typeParam.Constraints);

                        SimplifyAttributes(typeParam.CustomAttributes);
                    }

                    SimplifyAttributes(member.CustomAttributes);
                    SimplifyAttributes(member.ReturnTypeCustomAttributes);
                    
                    foreach (var parameter in member.Parameters.OfType<CodeParameterDeclarationExpression>())
                    {
                        SimplifyType(parameter.Type);
                        SimplifyAttributes(parameter.CustomAttributes);
                    }

                    SimplifyTypes(member.ImplementationTypes);
                    
                    foreach (var statement in member.Statements.OfType<CodeStatement>())
                    {
                        SimplifyStatement(statement);
                    }
                }
            }

            codeNamespace.Imports.AddRange(_imports.Select(i => new CodeNamespaceImport(i)).ToArray());
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
                foreach (var trueStatement in conditional.TrueStatements.OfType<CodeStatement>())
                {
                    SimplifyStatement(trueStatement);
                }
                foreach (var falseStatement in conditional.FalseStatements.OfType<CodeStatement>())
                {
                    SimplifyStatement(falseStatement);
                }
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
                foreach (var iterationStatement in iteration.Statements.OfType<CodeStatement>())
                {
                    SimplifyStatement(iterationStatement);
                }
            }

            var throwException = statement as CodeThrowExceptionStatement;
            if (throwException != null)
            {
                SimplifyExpression(throwException.ToThrow);
            }

            var tryStatement = statement as CodeTryCatchFinallyStatement;
            if (tryStatement != null)
            {
                foreach (var tried in tryStatement.TryStatements.OfType<CodeStatement>())
                {
                    SimplifyStatement(tried);
                }

                foreach (var catcher in tryStatement.CatchClauses.OfType<CodeCatchClause>())
                {
                    SimplifyType(catcher.CatchExceptionType);
                    foreach (var catchStatement in catcher.Statements.OfType<CodeStatement>())
                    {
                        SimplifyStatement(catchStatement);
                    }
                }

                foreach (var finallyStatement in tryStatement.FinallyStatements.OfType<CodeStatement>())
                {
                    SimplifyStatement(finallyStatement);
                }
            }
        }

        private void SimplifyExpression(CodeExpression expression)
        {
            var create = expression as CodeObjectCreateExpression;
            if (create != null)
            {
                SimplifyType(create.CreateType);
                foreach (var parameter in create.Parameters.OfType<CodeExpression>())
                {
                    SimplifyExpression(parameter);
                }
            }

            var arr = expression as CodeArrayCreateExpression;
            if (arr != null)
            {
                SimplifyType(arr.CreateType);
                foreach (var init in arr.Initializers.OfType<CodeExpression>())
                {
                    SimplifyExpression(init);
                }
                SimplifyExpression(arr.SizeExpression);
            }

            var arrInd = expression as CodeArrayIndexerExpression;
            if (arrInd != null)
            {
                foreach (var index in arrInd.Indices.OfType<CodeExpression>())
                {
                    SimplifyExpression(index);
                }
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
                foreach (var param in delegateInvoke.Parameters.OfType<CodeExpression>())
                {
                    SimplifyExpression(param);
                }
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
                if (fieldRef.FieldName.Contains('.'))
                {
                    var split = SplitNamespace(fieldRef.FieldName);

                    string ns = split.Item1;
                    string name = split.Item2;

                    if (!string.IsNullOrEmpty(ns))
                    {
                        _imports.Add(ns);
                        fieldRef.FieldName = name;
                    }
                }

                SimplifyExpression(fieldRef.TargetObject);
            }

            var indexer = expression as CodeIndexerExpression;
            if (indexer != null)
            {
                SimplifyExpression(indexer.TargetObject);
                foreach (var index in indexer.Indices.OfType<CodeExpression>())
                {
                    SimplifyExpression(index);
                }
            }

            var call = expression as CodeMethodInvokeExpression;
            if (call != null)
            {
                SimplifyMethodReference(call.Method);
                foreach (var parameter in call.Parameters.OfType<CodeExpression>())
                {
                    SimplifyExpression(parameter);
                }
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
                if (propRef.PropertyName.Contains('.'))
                {
                    var split = SplitNamespace(propRef.PropertyName);

                    string ns = split.Item1;
                    string name = split.Item2;

                    if (!string.IsNullOrEmpty(ns))
                    {
                        _imports.Add(ns);
                        propRef.PropertyName = name;
                    }
                }

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
            if (method.MethodName.Contains('.'))
            {
                var split = SplitNamespace(method.MethodName);

                string ns = split.Item1;
                string name = split.Item2;

                if (!string.IsNullOrEmpty(ns))
                {
                    _imports.Add(ns);
                    method.MethodName = name;
                }
            }

            SimplifyTypes(method.TypeArguments);

            SimplifyExpression(method.TargetObject);
        }

        private void SimplifyAttribute(CodeAttributeDeclaration instance)
        {
            var split = SplitNamespace(instance.Name);
            
            string ns = split.Item1;
            string name = split.Item2;
            
            if (name.EndsWith("Attribute", StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - 9);
            }

            if (!string.IsNullOrEmpty(ns))
            {
                _imports.Add(ns);
                instance.Name = name;
            }
            else
            {
                instance.Name = name;
            }
            
            SimplifyType(instance.AttributeType);

            foreach (var arg in instance.Arguments.OfType<CodeAttributeArgument>())
            {
                SimplifyArgument(arg);
            }
        }

        private void SimplifyArgument(CodeAttributeArgument arg)
        {
            var split = SplitNamespace(arg.Name);

            string ns = split.Item1;
            string name = split.Item2;

            if (!string.IsNullOrEmpty(ns))
            {
                _imports.Add(ns);
                arg.Name = name;
            }

            SimplifyExpression(arg.Value);
        }

        private void SimplifyType(CodeTypeReference type)
        {
            if (type == null)
            {
                return;
            }

            string typeName = type.BaseType;

            var split = SplitNamespace(typeName);

            string ns = split.Item1;
            string name = split.Item2;

            if (!string.IsNullOrEmpty(ns))
            {
                _imports.Add(ns);
                type.BaseType = name;
            }

            if (type.TypeArguments != null)
            {
                SimplifyTypes(type.TypeArguments);
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
