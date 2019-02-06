using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace QueryInterception
{
    public class TypeChangeVisitor : ExpressionVisitor
    {
        private readonly IDictionary<Type, Type> _typeReplacements;

        private int visitStack = 0;

        private readonly Dictionary<ParameterExpression, ParameterExpression> paramMappings = new Dictionary<ParameterExpression, ParameterExpression>();

        public NewExpression LastNew
        {
            get;
            private set;
        }

        public NewExpression LastNewResult
        {
            get;
            private set;
        }

        public TypeChangeVisitor(IDictionary<Type, Type> typeReplacements)
        {
            this._typeReplacements = typeReplacements;
            Dictionary<Type, Type> addItems = new Dictionary<Type, Type>();
            foreach (Type item in typeReplacements.Keys)
            {
                if (item.IsInterface)
                {
                    Type[] interfaces = item.GetInterfaces();
                    for (int num = 0; num < interfaces.Length; num++)
                    {
                        Type i = interfaces[num];
                        if (!this._typeReplacements.ContainsKey(i))
                        {
                            addItems.Add(i, this._typeReplacements[item]);
                        }
                    }
                }
            }
            foreach (KeyValuePair<Type, Type> item in addItems)
            {
                this._typeReplacements.Add(item.Key, item.Value);
            }
        }

        private bool NeedsTypeChange(Type t)
        {
            bool hasBadType = this._typeReplacements.Keys.Any<Type>((Type k) => t.FullName.Contains(k.FullName));
            return hasBadType;
        }

        private IEnumerable<Type> TransformMethodArgs(MethodBase method)
        {
            Type[] genericArguments = method.GetGenericArguments();
            for (int i = 0; i < genericArguments.Length; i++)
            {
                yield return this.VisitType(genericArguments[i]);
            }
        }

        private MethodCallExpression TransformMethodCall(MethodCallExpression node)
        {
            Debug.Assert(node.Method != null);
            Type[] argTypes = this.TransformMethodArgs(node.Method).ToArray<Type>();
            Debug.Assert(!argTypes.Any<Type>(new Func<Type, bool>(this.NeedsTypeChange)));
            Expression[] argParams = (
                from n in node.Arguments
                select this.Visit(n)).ToArray<Expression>();
            Debug.Assert(!argParams.Any<Expression>((Expression a) => this.NeedsTypeChange(a.Type)));
            MethodInfo methodInfo = node.Method.GetGenericMethodDefinition().MakeGenericMethod(argTypes);
            Debug.Assert(!this.NeedsTypeChange(methodInfo.DeclaringType));
            MethodCallExpression visited = Expression.Call(node.Object, methodInfo, argParams);
            Debug.Assert(!this.NeedsTypeChange(visited.Type));
            return visited;
        }

        private NewExpression TransformNewCall(NewExpression node)
        {
            Debug.Assert(node.Constructor != null);
            IEnumerable<Expression> argParams =
                from n in node.Arguments
                select this.Visit(n);
            Debug.Assert(!argParams.Any<Expression>((Expression a) => this.NeedsTypeChange(a.Type)));
            ConstructorInfo constructor = node.Constructor;
            if ((!constructor.DeclaringType.IsGenericType ? false : constructor.DeclaringType.GetGenericArguments().Any<Type>(new Func<Type, bool>(this.NeedsTypeChange))))
            {
                Type newType = this.VisitType(constructor.DeclaringType);
                Debug.Assert(!this.NeedsTypeChange(newType));
                Type[] constructorTypes = (
                    from s in constructor.GetParameters()
                    select s.ParameterType).Select<Type, Type>(new Func<Type, Type>(this.VisitType)).ToArray<Type>();
                Debug.Assert(!constructorTypes.Any<Type>(new Func<Type, bool>(this.NeedsTypeChange)));
                constructor = newType.GetConstructor(constructorTypes);
                Debug.Assert(!this.NeedsTypeChange(constructor.DeclaringType));
            }
            ReadOnlyCollection<MemberInfo> memberInfos = node.Members;
            MemberInfo[] memberInfoArray = constructor.DeclaringType.GetMembers();
            Func<MemberInfo, string> name = (MemberInfo fMember) => fMember.Name;
            Func<MemberInfo, string> func = (MemberInfo nMember) => nMember.Name;
            IEnumerable<MemberInfo> members = memberInfos.Join<MemberInfo, MemberInfo, string, MemberInfo>(memberInfoArray, name, func, (MemberInfo fMember, MemberInfo nMember) => nMember);
            MemberInfo[] membersTransformed = members.ToArray<MemberInfo>();
            NewExpression visited = Expression.New(constructor, argParams, membersTransformed);
            Debug.Assert(visited.Members.Count == node.Members.Count);
            Debug.Assert(!this.NeedsTypeChange(visited.Type));
            return visited;
        }

        public override Expression Visit(Expression node)
        {
            Expression expression;
            if (node != null)
            {
                Expression found = null;
                TypeChangeVisitor typeChangeVisitor = this;
                typeChangeVisitor.visitStack = typeChangeVisitor.visitStack + 1;
                found = base.Visit(node);
                Debug.Assert((found == null ? true : !this.NeedsTypeChange(found.Type)));
                TypeChangeVisitor typeChangeVisitor1 = this;
                typeChangeVisitor1.visitStack = typeChangeVisitor1.visitStack - 1;
                expression = found;
            }
            else
            {
                expression = null;
            }
            return expression;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            return base.VisitBinary(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            Expression visited;
            Expression expression;
            if (!this.NeedsTypeChange(node.Type))
            {
                visited = base.VisitConstant(node);
            }
            else
            {
                if (node.Value == null)
                {
                    expression = Expression.Constant(null, this.VisitType(node.Type));
                    return expression;
                }
                Type valueType = node.Value.GetType();
                if (!valueType.IsArray)
                {
                    visited = Expression.Constant(node.Value);
                }
                else
                {
                    object value = node.Value;
                    if (!this.NeedsTypeChange(valueType))
                    {
                        Debug.Assert(false);
                    }
                    else
                    {
                        Type newElementType = this.VisitType(valueType.GetElementType());
                        Array oldArray = node.Value as Array;
                        Array newArray = Array.CreateInstance(newElementType, oldArray.Length);
                        oldArray.CopyTo(newArray, 0);
                        value = newArray;
                    }
                    visited = base.VisitConstant(Expression.Constant(value));
                }
            }
            Debug.Assert(!this.NeedsTypeChange(visited.Type));
            expression = visited;
            return expression;
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            return base.VisitInvocation(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Expression visited;
            if ((this.NeedsTypeChange(node.ReturnType) ? false : !node.Parameters.Any<ParameterExpression>((ParameterExpression p) => this.NeedsTypeChange(p.Type))))
            {
                visited = base.VisitLambda<T>(node);
            }
            else
            {
                Expression visitedBody = this.Visit(node.Body);
                Debug.Assert(!this.NeedsTypeChange(visitedBody.Type));
                IList<ParameterExpression> transformedParams = new List<ParameterExpression>();
                foreach (ParameterExpression parameter in node.Parameters)
                {
                    Expression transformedParam = this.VisitParameter(parameter);
                    Debug.Assert(transformedParam is ParameterExpression);
                    transformedParams.Add((ParameterExpression)transformedParam);
                }
                Debug.Assert(!transformedParams.Any<ParameterExpression>((ParameterExpression t) => this.NeedsTypeChange(t.Type)));
                LambdaExpression transformed = Expression.Lambda(visitedBody, transformedParams.ToArray<ParameterExpression>());
                Debug.Assert(!this.NeedsTypeChange(transformed.Type));
                Debug.Assert(!this.NeedsTypeChange(transformed.ReturnType));
                if (!(transformed is Expression<T>))
                {
                    visited = transformed;
                }
                else
                {
                    visited = base.VisitLambda<T>(transformed as Expression<T>);
                }
            }
            Debug.Assert(!this.NeedsTypeChange(visited.Type));
            return visited;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            Expression visited = node;
            if ((this.NeedsTypeChange(node.Type) ? false : !this.NeedsTypeChange(node.Member.DeclaringType)))
            {
                visited = base.VisitMember(node);
            }
            else
            {
                Type newtype = this.VisitType(node.Member.DeclaringType);
                Expression visitedExpression = this.Visit(node.Expression);
                Debug.Assert(!this.NeedsTypeChange(visitedExpression.Type));
                MethodInfo targetProperty = newtype.GetProperty(node.Member.Name).GetGetMethod();
                visited = Expression.Property(visitedExpression, targetProperty);
                Debug.Assert(!this.NeedsTypeChange(visited.Type));
                visited = base.VisitMember((MemberExpression)visited);
            }
            Debug.Assert(!this.NeedsTypeChange(visited.Type));
            return visited;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Expression visited;
            MethodCallExpression transformed;
            if (!(!(node.Method != null) || !(node.Method.ReturnType != null) ? true : !this.NeedsTypeChange(node.Method.ReturnType)))
            {
                transformed = this.TransformMethodCall(node);
                Debug.WriteLine("Transformed methodcall");
                visited = base.VisitMethodCall(transformed);
            }
            else if ((!(node.Method != null) || node.Arguments == null ? true : !node.Arguments.Any<Expression>((Expression t) => this.NeedsTypeChange(t.Type))))
            {
                visited = base.VisitMethodCall(node);
            }
            else
            {
                transformed = this.TransformMethodCall(node);
                Debug.WriteLine("Transformed methodcall");
                visited = base.VisitMethodCall(transformed);
            }
            return visited;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            Expression visited;
            this.LastNew = node;
            Debug.WriteLine("Transforming NewExpression");
            Debug.WriteLine(ExpressionWriter.WriteToString(node));
            Debug.WriteLine(string.Empty);
            if ((this.NeedsTypeChange(node.Type) ? false : !node.Arguments.Any<Expression>((Expression a) => this.NeedsTypeChange(a.Type))))
            {
                visited = base.VisitNew(node);
            }
            else
            {
                NewExpression transformed = this.TransformNewCall(node);
                Debug.Assert(!this.NeedsTypeChange(transformed.Type));
                visited = base.VisitNew(transformed);
            }
            Debug.WriteLine("Transformed to");
            Debug.WriteLine(ExpressionWriter.WriteToString(visited));
            Debug.WriteLine(string.Empty);
            this.LastNewResult = (NewExpression)visited;
            return visited;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Expression visited = node;
            if (!this.NeedsTypeChange(node.Type))
            {
                visited = base.VisitParameter(node);
            }
            else
            {
                ParameterExpression visitedParam = node;
                if (!this.paramMappings.ContainsKey(node))
                {
                    Type newType = this.VisitType(visitedParam.Type);
                    ParameterExpression newParam = Expression.Parameter(newType, node.Name);
                    this.paramMappings.Add(node, newParam);
                    visited = base.VisitParameter(newParam);
                }
                else
                {
                    visited = base.VisitParameter(this.paramMappings[node]);
                }
            }
            Debug.Assert(!this.NeedsTypeChange(visited.Type));
            return visited;
        }

        private Type VisitType(Type t)
        {
            Type item;
            if (this._typeReplacements.ContainsKey(t))
            {
                item = this._typeReplacements[t];
            }
            else if (!(t.IsGenericType & t.GetGenericArguments().Any<Type>(new Func<Type, bool>(this.NeedsTypeChange))))
            {
                Debug.Assert(!this.NeedsTypeChange(t));
                item = t;
            }
            else
            {
                Type[] types = t.GetGenericArguments().Select<Type, Type>(new Func<Type, Type>(this.VisitType)).ToArray<Type>();
                Type newType = t.GetGenericTypeDefinition().MakeGenericType(types);
                Debug.Assert(!this.NeedsTypeChange(newType));
                item = newType;
            }
            return item;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            Expression visited;
            if (!this.NeedsTypeChange(node.Type))
            {
                visited = base.VisitUnary(node);
            }
            else
            {
                Expression operand = this.Visit(node.Operand);
                Type newType = this.VisitType(node.Type);
                visited = Expression.MakeUnary(node.NodeType, operand, newType);
            }
            Debug.Assert(!this.NeedsTypeChange(visited.Type));
            return visited;
        }
    }
}