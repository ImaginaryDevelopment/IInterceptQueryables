using QueryInterception.IQ;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace QueryInterception
{
    public class ExpressionWriter : IqExpressionVisitor
    {
        readonly TextWriter writer;

        int depth;

        readonly static char[] splitters;

        readonly static char[] special;

        protected int IndentationWidth { get; set; } = 2;

        static ExpressionWriter()
        {
            ExpressionWriter.splitters = new char[] { '\n', '\r' };
            ExpressionWriter.special = new char[] { '\n', '\n', '\\' };
        }

        public ExpressionWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        protected virtual string GetOperator(ExpressionType type)
        {
            string str;
            switch (type)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    {
                        str = "+";
                        break;
                    }
                case ExpressionType.And:
                    {
                        str = "&";
                        break;
                    }
                case ExpressionType.AndAlso:
                    {
                        str = "&&";
                        break;
                    }
                case ExpressionType.ArrayLength:
                case ExpressionType.ArrayIndex:
                case ExpressionType.Call:
                case ExpressionType.Conditional:
                case ExpressionType.Constant:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Invoke:
                case ExpressionType.Lambda:
                case ExpressionType.ListInit:
                case ExpressionType.MemberAccess:
                case ExpressionType.MemberInit:
                case ExpressionType.UnaryPlus:
                case ExpressionType.New:
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                case ExpressionType.Parameter:
                case ExpressionType.Power:
                case ExpressionType.Quote:
                    {
                        str = null;
                        break;
                    }
                case ExpressionType.Coalesce:
                    {
                        str = "??";
                        break;
                    }
                case ExpressionType.Divide:
                    {
                        str = "/";
                        break;
                    }
                case ExpressionType.Equal:
                    {
                        str = "==";
                        break;
                    }
                case ExpressionType.ExclusiveOr:
                    {
                        str = "^";
                        break;
                    }
                case ExpressionType.GreaterThan:
                    {
                        str = ">";
                        break;
                    }
                case ExpressionType.GreaterThanOrEqual:
                    {
                        str = ">=";
                        break;
                    }
                case ExpressionType.LeftShift:
                    {
                        str = "<<";
                        break;
                    }
                case ExpressionType.LessThan:
                    {
                        str = "<";
                        break;
                    }
                case ExpressionType.LessThanOrEqual:
                    {
                        str = "<=";
                        break;
                    }
                case ExpressionType.Modulo:
                    {
                        str = "%";
                        break;
                    }
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    {
                        str = "*";
                        break;
                    }
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    {
                        str = "-";
                        break;
                    }
                case ExpressionType.Not:
                    {
                        str = "!";
                        break;
                    }
                case ExpressionType.NotEqual:
                    {
                        str = "!=";
                        break;
                    }
                case ExpressionType.Or:
                    {
                        str = "|";
                        break;
                    }
                case ExpressionType.OrElse:
                    {
                        str = "||";
                        break;
                    }
                case ExpressionType.RightShift:
                    {
                        str = ">>";
                        break;
                    }
                default:
                    {
                        goto case ExpressionType.Quote;
                    }
            }
            return str;
        }

        protected virtual string GetTypeName(Type type)
        {
            string name = type.Name;
            name = name.Replace('+', '.');
            int iGeneneric = name.IndexOf('\u0060');
            if (iGeneneric > 0)
            {
                name = name.Substring(0, iGeneneric);
            }
            if ((type.IsGenericType ? true : type.IsGenericTypeDefinition))
            {
                var sb = new StringBuilder();
                sb.Append(name);
                sb.Append("<");
                Type[] args = type.GetGenericArguments();
                int i = 0;
                int n = args.Length;
                while (i < n)
                {
                    if (i > 0)
                    {
                        sb.Append(",");
                    }
                    if (type.IsGenericType)
                    {
                        sb.Append(this.GetTypeName(args[i]));
                    }
                    i++;
                }
                sb.Append(">");
                name = sb.ToString();
            }
            return name;
        }

        protected void Indent(ExpressionWriter.Indentation style)
        {
            if (style == ExpressionWriter.Indentation.Inner)
            {
                ExpressionWriter expressionWriter = this;
                expressionWriter.depth = expressionWriter.depth + 1;
            }
            else if (style == ExpressionWriter.Indentation.Outer)
            {
                ExpressionWriter expressionWriter1 = this;
                expressionWriter1.depth = expressionWriter1.depth - 1;
                Debug.Assert(this.depth >= 0);
            }
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            ExpressionType nodeType = b.NodeType;
            if (nodeType == ExpressionType.ArrayIndex)
            {
                this.Visit(b.Left);
                this.Write("[");
                this.Visit(b.Right);
                this.Write("]");
            }
            else if (nodeType == ExpressionType.Power)
            {
                this.Write("POW(");
                this.Visit(b.Left);
                this.Write(", ");
                this.Visit(b.Right);
                this.Write(")");
            }
            else
            {
                this.Visit(b.Left);
                this.Write(" ");
                this.Write(this.GetOperator(b.NodeType));
                this.Write(" ");
                this.Visit(b.Right);
            }
            return b;
        }

        protected override IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            int i = 0;
            int n = original.Count;
            while (i < n)
            {
                this.VisitBinding(original[i]);
                if (i < n - 1)
                {
                    this.Write(",");
                    this.WriteLine(ExpressionWriter.Indentation.Same);
                }
                i++;
            }
            return original;
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            this.Visit(c.Test);
            this.WriteLine(ExpressionWriter.Indentation.Inner);
            this.Write("? ");
            this.Visit(c.IfTrue);
            this.WriteLine(ExpressionWriter.Indentation.Same);
            this.Write(": ");
            this.Visit(c.IfFalse);
            this.Indent(ExpressionWriter.Indentation.Outer);
            return c;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value == null)
            {
                this.Write("null");
            }
            else if (c.Type == typeof(string))
            {
                if (c.Value.ToString().IndexOfAny(ExpressionWriter.special) >= 0)
                {
                    this.Write("@");
                }
                this.Write("\"");
                this.Write(c.Value.ToString());
                this.Write("\"");
            }
            else if (c.Type == typeof(DateTime))
            {
                this.Write("new DateTime(\"");
                this.Write(c.Value.ToString());
                this.Write("\")");
            }
            else if (!c.Type.IsArray)
            {
                this.Write(c.Value.ToString());
            }
            else
            {
                Type elementType = c.Type.GetElementType();
                this.VisitNewArray(Expression.NewArrayInit(elementType,
                    from v in ((IEnumerable)c.Value).OfType<object>()
                    select Expression.Constant(v, elementType)));
            }
            return c;
        }

        protected override ElementInit VisitElementInitializer(ElementInit initializer)
        {
            if (initializer.Arguments.Count <= 1)
            {
                this.Visit(initializer.Arguments[0]);
            }
            else
            {
                this.Write("{");
                int i = 0;
                int n = initializer.Arguments.Count;
                while (i < n)
                {
                    this.Visit(initializer.Arguments[i]);
                    if (i < n - 1)
                    {
                        this.Write(", ");
                    }
                    i++;
                }
                this.Write("}");
            }
            return initializer;
        }

        protected override IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
        {
            int i = 0;
            int n = original.Count;
            while (i < n)
            {
                this.VisitElementInitializer(original[i]);
                if (i < n - 1)
                {
                    this.Write(",");
                    this.WriteLine(ExpressionWriter.Indentation.Same);
                }
                i++;
            }
            return original;
        }

        protected override ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            int i = 0;
            int n = original.Count;
            while (i < n)
            {
                this.Visit(original[i]);
                if (i < n - 1)
                {
                    this.Write(",");
                    this.WriteLine(ExpressionWriter.Indentation.Same);
                }
                i++;
            }
            return original;
        }

        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            this.Write("Invoke(");
            this.WriteLine(ExpressionWriter.Indentation.Inner);
            this.VisitExpressionList(iv.Arguments);
            this.Write(", ");
            this.WriteLine(ExpressionWriter.Indentation.Same);
            this.Visit(iv.Expression);
            this.WriteLine(ExpressionWriter.Indentation.Same);
            this.Write(")");
            this.Indent(ExpressionWriter.Indentation.Outer);
            return iv;
        }

        protected override Expression VisitLambda(LambdaExpression lambda)
        {
            if (lambda.Parameters.Count == 1)
            {
                this.Write(lambda.Parameters[0].Name);
            }
            else
            {
                this.Write("(");
                int i = 0;
                int n = lambda.Parameters.Count;
                while (i < n)
                {
                    this.Write(lambda.Parameters[i].Name);
                    if (i < n - 1)
                    {
                        this.Write(", ");
                    }
                    i++;
                }
                this.Write(")");
            }
            this.Write(" => ");
            this.Visit(lambda.Body);
            return lambda;
        }

        protected override Expression VisitListInit(ListInitExpression init)
        {
            this.Visit(init.NewExpression);
            this.Write(" {");
            this.WriteLine(ExpressionWriter.Indentation.Inner);
            this.VisitElementInitializerList(init.Initializers);
            this.WriteLine(ExpressionWriter.Indentation.Outer);
            this.Write("}");
            return init;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            this.Visit(m.Expression);
            this.Write(".");
            this.Write(m.Member.Name);
            return m;
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            this.Write(assignment.Member.Name);
            this.Write(" = ");
            this.Visit(assignment.Expression);
            return assignment;
        }

        protected override Expression VisitMemberInit(MemberInitExpression init)
        {
            this.Visit(init.NewExpression);
            this.Write(" {");
            this.WriteLine(ExpressionWriter.Indentation.Inner);
            this.VisitBindingList(init.Bindings);
            this.WriteLine(ExpressionWriter.Indentation.Outer);
            this.Write("}");
            return init;
        }

        protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
        {
            this.Write(binding.Member.Name);
            this.Write(" = {");
            this.WriteLine(ExpressionWriter.Indentation.Inner);
            this.VisitElementInitializerList(binding.Initializers);
            this.WriteLine(ExpressionWriter.Indentation.Outer);
            this.Write("}");
            return binding;
        }

        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            this.Write(binding.Member.Name);
            this.Write(" = {");
            this.WriteLine(ExpressionWriter.Indentation.Inner);
            this.VisitBindingList(binding.Bindings);
            this.WriteLine(ExpressionWriter.Indentation.Outer);
            this.Write("}");
            return binding;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Object == null)
            {
                this.Write(this.GetTypeName(m.Method.DeclaringType));
            }
            else
            {
                this.Visit(m.Object);
            }
            this.Write(".");
            this.Write(m.Method.Name);
            this.Write("(");
            if (m.Arguments.Count > 1)
            {
                this.WriteLine(ExpressionWriter.Indentation.Inner);
            }
            this.VisitExpressionList(m.Arguments);
            if (m.Arguments.Count > 1)
            {
                this.WriteLine(ExpressionWriter.Indentation.Outer);
            }
            this.Write(")");
            return m;
        }

        protected override NewExpression VisitNew(NewExpression nex)
        {
            this.Write("new ");
            this.Write(this.GetTypeName(nex.Constructor.DeclaringType));
            this.Write("(");
            if (nex.Arguments.Count > 1)
            {
                this.WriteLine(ExpressionWriter.Indentation.Inner);
            }
            this.VisitExpressionList(nex.Arguments);
            if (nex.Arguments.Count > 1)
            {
                this.WriteLine(ExpressionWriter.Indentation.Outer);
            }
            this.Write(")");
            return nex;
        }

        protected override Expression VisitNewArray(NewArrayExpression na)
        {
            this.Write("new ");
            this.Write(this.GetTypeName(QueryInterception.TypeHelper.GetElementType(na.Type)));
            this.Write("[] {");
            if (na.Expressions.Count > 1)
            {
                this.WriteLine(ExpressionWriter.Indentation.Inner);
            }
            this.VisitExpressionList(na.Expressions);
            if (na.Expressions.Count > 1)
            {
                this.WriteLine(ExpressionWriter.Indentation.Outer);
            }
            this.Write("}");
            return na;
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            this.Write(p.Name);
            return p;
        }

        protected override Expression VisitTypeIs(TypeBinaryExpression b)
        {
            this.Visit(b.Expression);
            this.Write(" is ");
            this.Write(this.GetTypeName(b.TypeOperand));
            return b;
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            Expression expression;
            Expression expression1;
            ExpressionType nodeType = u.NodeType;
            if (nodeType <= ExpressionType.ConvertChecked)
            {
                if (nodeType == ExpressionType.ArrayLength)
                {
                    this.Visit(u.Operand);
                    this.Write(".Length");
                }
                else
                {
                    switch (nodeType)
                    {
                        case ExpressionType.Convert:
                        case ExpressionType.ConvertChecked:
                            {
                                this.Write("((");
                                this.Write(this.GetTypeName(u.Type));
                                this.Write(")");
                                this.Visit(u.Operand);
                                this.Write(")");
                                break;
                            }
                        default:
                            {
                                this.Write(this.GetOperator(u.NodeType));
                                expression1 = this.Visit(u.Operand);
                                expression = u;
                                return expression;
                            }
                    }
                }
            }
            else if (nodeType == ExpressionType.UnaryPlus)
            {
                this.Visit(u.Operand);
            }
            else if (nodeType == ExpressionType.Quote)
            {
                this.Visit(u.Operand);
            }
            else
            {
                if (nodeType != ExpressionType.TypeAs)
                {
                    this.Write(this.GetOperator(u.NodeType));
                    expression1 = this.Visit(u.Operand);
                    expression = u;
                    return expression;
                }
                this.Visit(u.Operand);
                this.Write(" as ");
                this.Write(this.GetTypeName(u.Type));
            }
            expression = u;
            return expression;
        }

        protected override Expression VisitUnknown(Expression expression)
        {
            this.Write(expression.ToString());
            return expression;
        }

        public static void Write(TextWriter writer, Expression expression)
        {
            (new ExpressionWriter(writer)).Visit(expression);
        }

        protected void Write(string text)
        {
            if (text.IndexOf('\n') < 0)
            {
                this.writer.Write(text);
            }
            else
            {
                string[] lines = text.Split(ExpressionWriter.splitters, StringSplitOptions.RemoveEmptyEntries);
                int i = 0;
                int n = lines.Length;
                while (i < n)
                {
                    this.Write(lines[i]);
                    if (i < n - 1)
                    {
                        this.WriteLine(ExpressionWriter.Indentation.Same);
                    }
                    i++;
                }
            }
        }

        protected void WriteLine(ExpressionWriter.Indentation style)
        {
            this.writer.WriteLine();
            this.Indent(style);
            int i = 0;
            int n = this.depth * this.IndentationWidth;
            while (i < n)
            {
                this.writer.Write(" ");
                i++;
            }
        }

        public static string WriteToString(Expression expression)
        {
            StringWriter sw = new StringWriter();
            ExpressionWriter.Write(sw, expression);
            return sw.ToString();
        }

        protected enum Indentation
        {
            Same,
            Inner,
            Outer
        }
    }
}