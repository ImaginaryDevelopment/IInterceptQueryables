using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryInterception.IQ
{
	public abstract class IqExpressionVisitor : ExpressionVisitor
	{
		protected IqExpressionVisitor()
		{
		}

		protected BinaryExpression UpdateBinary(BinaryExpression b, Expression left, Expression right, Expression conversion, bool isLiftedToNull, MethodInfo method)
		{
			BinaryExpression binaryExpression;
			if ((left != b.Left || right != b.Right || conversion != b.Conversion || method != b.Method ? false : isLiftedToNull == b.IsLiftedToNull))
			{
				binaryExpression = b;
			}
			else
			{
				binaryExpression = ((b.NodeType != ExpressionType.Coalesce ? true : b.Conversion == null) ? Expression.MakeBinary(b.NodeType, left, right, isLiftedToNull, method) : Expression.Coalesce(left, right, conversion as LambdaExpression));
			}
			return binaryExpression;
		}

		protected ConditionalExpression UpdateConditional(ConditionalExpression c, Expression test, Expression ifTrue, Expression ifFalse)
		{
			ConditionalExpression conditionalExpression;
			conditionalExpression = ((test != c.Test || ifTrue != c.IfTrue ? false : ifFalse == c.IfFalse) ? c : Expression.Condition(test, ifTrue, ifFalse));
			return conditionalExpression;
		}

		protected InvocationExpression UpdateInvocation(InvocationExpression iv, Expression expression, IEnumerable<Expression> args)
		{
			InvocationExpression invocationExpression;
			invocationExpression = ((args != iv.Arguments ? false : expression == iv.Expression) ? iv : Expression.Invoke(expression, args));
			return invocationExpression;
		}

		protected LambdaExpression UpdateLambda(LambdaExpression lambda, Type delegateType, Expression body, IEnumerable<ParameterExpression> parameters)
		{
			LambdaExpression lambdaExpression;
			lambdaExpression = ((body != lambda.Body || parameters != lambda.Parameters ? false : !(delegateType != lambda.Type)) ? lambda : Expression.Lambda(delegateType, body, parameters));
			return lambdaExpression;
		}

		protected ListInitExpression UpdateListInit(ListInitExpression init, NewExpression nex, IEnumerable<ElementInit> initializers)
		{
			ListInitExpression listInitExpression;
			listInitExpression = ((nex != init.NewExpression ? false : initializers == init.Initializers) ? init : Expression.ListInit(nex, initializers));
			return listInitExpression;
		}

		protected MemberExpression UpdateMemberAccess(MemberExpression m, Expression expression, MemberInfo member)
		{
			MemberExpression memberExpression;
			memberExpression = ((expression != m.Expression ? false : !(member != m.Member)) ? m : Expression.MakeMemberAccess(expression, member));
			return memberExpression;
		}

		protected MemberAssignment UpdateMemberAssignment(MemberAssignment assignment, MemberInfo member, Expression expression)
		{
			MemberAssignment memberAssignment;
			memberAssignment = ((expression != assignment.Expression ? false : !(member != assignment.Member)) ? assignment : Expression.Bind(member, expression));
			return memberAssignment;
		}

		protected MemberInitExpression UpdateMemberInit(MemberInitExpression init, NewExpression nex, IEnumerable<MemberBinding> bindings)
		{
			MemberInitExpression memberInitExpression;
			memberInitExpression = ((nex != init.NewExpression ? false : bindings == init.Bindings) ? init : Expression.MemberInit(nex, bindings));
			return memberInitExpression;
		}

		protected MemberListBinding UpdateMemberListBinding(MemberListBinding binding, MemberInfo member, IEnumerable<ElementInit> initializers)
		{
			MemberListBinding memberListBinding;
			memberListBinding = ((initializers != binding.Initializers ? false : !(member != binding.Member)) ? binding : Expression.ListBind(member, initializers));
			return memberListBinding;
		}

		protected MemberMemberBinding UpdateMemberMemberBinding(MemberMemberBinding binding, MemberInfo member, IEnumerable<MemberBinding> bindings)
		{
			MemberMemberBinding memberMemberBinding;
			memberMemberBinding = ((bindings != binding.Bindings ? false : !(member != binding.Member)) ? binding : Expression.MemberBind(member, bindings));
			return memberMemberBinding;
		}

		protected MethodCallExpression UpdateMethodCall(MethodCallExpression m, Expression obj, MethodInfo method, IEnumerable<Expression> args)
		{
			MethodCallExpression methodCallExpression;
			methodCallExpression = ((obj != m.Object || method != m.Method ? false : args == m.Arguments) ? m : Expression.Call(obj, method, args));
			return methodCallExpression;
		}

		protected NewExpression UpdateNew(NewExpression nex, ConstructorInfo constructor, IEnumerable<Expression> args, IEnumerable<MemberInfo> members)
		{
			NewExpression newExpression;
			if ((args != nex.Arguments || constructor != nex.Constructor ? false : members == nex.Members))
			{
				newExpression = nex;
			}
			else
			{
				newExpression = (nex.Members == null ? Expression.New(constructor, args) : Expression.New(constructor, args, members));
			}
			return newExpression;
		}

		protected NewArrayExpression UpdateNewArray(NewArrayExpression na, Type arrayType, IEnumerable<Expression> expressions)
		{
			NewArrayExpression newArrayExpression;
			if ((expressions != na.Expressions ? false : !(na.Type != arrayType)))
			{
				newArrayExpression = na;
			}
			else
			{
				newArrayExpression = (na.NodeType != ExpressionType.NewArrayInit ? Expression.NewArrayBounds(arrayType.GetElementType(), expressions) : Expression.NewArrayInit(arrayType.GetElementType(), expressions));
			}
			return newArrayExpression;
		}

		protected TypeBinaryExpression UpdateTypeIs(TypeBinaryExpression b, Expression expression, Type typeOperand)
		{
			TypeBinaryExpression typeBinaryExpression;
			typeBinaryExpression = ((expression != b.Expression ? false : !(typeOperand != b.TypeOperand)) ? b : Expression.TypeIs(expression, typeOperand));
			return typeBinaryExpression;
		}

		protected UnaryExpression UpdateUnary(UnaryExpression u, Expression operand, Type resultType, MethodInfo method)
		{
			UnaryExpression unaryExpression;
			unaryExpression = ((u.Operand != operand || u.Type != resultType ? false : !(u.Method != method)) ? u : Expression.MakeUnary(u.NodeType, operand, resultType, method));
			return unaryExpression;
		}

		public override Expression Visit(Expression exp)
		{
			Expression expression;
			if (exp != null)
			{
				switch (exp.NodeType)
				{
					case ExpressionType.Add:
					case ExpressionType.AddChecked:
					case ExpressionType.And:
					case ExpressionType.AndAlso:
					case ExpressionType.ArrayIndex:
					case ExpressionType.Coalesce:
					case ExpressionType.Divide:
					case ExpressionType.Equal:
					case ExpressionType.ExclusiveOr:
					case ExpressionType.GreaterThan:
					case ExpressionType.GreaterThanOrEqual:
					case ExpressionType.LeftShift:
					case ExpressionType.LessThan:
					case ExpressionType.LessThanOrEqual:
					case ExpressionType.Modulo:
					case ExpressionType.Multiply:
					case ExpressionType.MultiplyChecked:
					case ExpressionType.NotEqual:
					case ExpressionType.Or:
					case ExpressionType.OrElse:
					case ExpressionType.Power:
					case ExpressionType.RightShift:
					case ExpressionType.Subtract:
					case ExpressionType.SubtractChecked:
					{
						expression = this.VisitBinary((BinaryExpression)exp);
						break;
					}
					case ExpressionType.ArrayLength:
					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
					case ExpressionType.Negate:
					case ExpressionType.UnaryPlus:
					case ExpressionType.NegateChecked:
					case ExpressionType.Not:
					case ExpressionType.Quote:
					case ExpressionType.TypeAs:
					{
						expression = this.VisitUnary((UnaryExpression)exp);
						break;
					}
					case ExpressionType.Call:
					{
						expression = this.VisitMethodCall((MethodCallExpression)exp);
						break;
					}
					case ExpressionType.Conditional:
					{
						expression = this.VisitConditional((ConditionalExpression)exp);
						break;
					}
					case ExpressionType.Constant:
					{
						expression = this.VisitConstant((ConstantExpression)exp);
						break;
					}
					case ExpressionType.Invoke:
					{
						expression = this.VisitInvocation((InvocationExpression)exp);
						break;
					}
					case ExpressionType.Lambda:
					{
						expression = this.VisitLambda((LambdaExpression)exp);
						break;
					}
					case ExpressionType.ListInit:
					{
						expression = this.VisitListInit((ListInitExpression)exp);
						break;
					}
					case ExpressionType.MemberAccess:
					{
						expression = this.VisitMemberAccess((MemberExpression)exp);
						break;
					}
					case ExpressionType.MemberInit:
					{
						expression = this.VisitMemberInit((MemberInitExpression)exp);
						break;
					}
					case ExpressionType.New:
					{
						expression = this.VisitNew((NewExpression)exp);
						break;
					}
					case ExpressionType.NewArrayInit:
					case ExpressionType.NewArrayBounds:
					{
						expression = this.VisitNewArray((NewArrayExpression)exp);
						break;
					}
					case ExpressionType.Parameter:
					{
						expression = this.VisitParameter((ParameterExpression)exp);
						break;
					}
					case ExpressionType.TypeIs:
					{
						expression = this.VisitTypeIs((TypeBinaryExpression)exp);
						break;
					}
					default:
					{
						expression = this.VisitUnknown(exp);
						break;
					}
				}
			}
			else
			{
				expression = exp;
			}
			return expression;
		}

		protected virtual new Expression VisitBinary(BinaryExpression b)
		{
			Expression left = this.Visit(b.Left);
			Expression right = this.Visit(b.Right);
			Expression conversion = this.Visit(b.Conversion);
			Expression expression = this.UpdateBinary(b, left, right, conversion, b.IsLiftedToNull, b.Method);
			return expression;
		}

		protected virtual MemberBinding VisitBinding(MemberBinding binding)
		{
			MemberBinding memberBinding;
			switch (binding.BindingType)
			{
				case MemberBindingType.Assignment:
				{
					memberBinding = this.VisitMemberAssignment((MemberAssignment)binding);
					break;
				}
				case MemberBindingType.MemberBinding:
				{
					memberBinding = this.VisitMemberMemberBinding((MemberMemberBinding)binding);
					break;
				}
				case MemberBindingType.ListBinding:
				{
					memberBinding = this.VisitMemberListBinding((MemberListBinding)binding);
					break;
				}
				default:
				{
					throw new Exception(string.Format("Unhandled binding type '{0}'", binding.BindingType));
				}
			}
			return memberBinding;
		}

		protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
		{
			IEnumerable<MemberBinding> memberBindings;
			List<MemberBinding> list = null;
			int i = 0;
			int n = original.Count;
			while (i < n)
			{
				MemberBinding b = this.VisitBinding(original[i]);
				if (list != null)
				{
					list.Add(b);
				}
				else if (b != original[i])
				{
					list = new List<MemberBinding>(n);
					for (int j = 0; j < i; j++)
					{
						list.Add(original[j]);
					}
					list.Add(b);
				}
				i++;
			}
			if (list == null)
			{
				memberBindings = original;
			}
			else
			{
				memberBindings = list;
			}
			return memberBindings;
		}

		protected virtual new Expression VisitConditional(ConditionalExpression c)
		{
			Expression test = this.Visit(c.Test);
			Expression ifTrue = this.Visit(c.IfTrue);
			Expression ifFalse = this.Visit(c.IfFalse);
			return this.UpdateConditional(c, test, ifTrue, ifFalse);
		}

		protected virtual new Expression VisitConstant(ConstantExpression c)
		{
			return c;
		}

		protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
		{
			ElementInit elementInit;
			ReadOnlyCollection<Expression> arguments = this.VisitExpressionList(initializer.Arguments);
			elementInit = (arguments == initializer.Arguments ? initializer : Expression.ElementInit(initializer.AddMethod, arguments));
			return elementInit;
		}

		protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
		{
			IEnumerable<ElementInit> elementInits;
			List<ElementInit> list = null;
			int i = 0;
			int n = original.Count;
			while (i < n)
			{
				ElementInit init = this.VisitElementInitializer(original[i]);
				if (list != null)
				{
					list.Add(init);
				}
				else if (init != original[i])
				{
					list = new List<ElementInit>(n);
					for (int j = 0; j < i; j++)
					{
						list.Add(original[j]);
					}
					list.Add(init);
				}
				i++;
			}
			if (list == null)
			{
				elementInits = original;
			}
			else
			{
				elementInits = list;
			}
			return elementInits;
		}

		protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
		{
			ReadOnlyCollection<Expression> expressions;
			if (original != null)
			{
				List<Expression> list = null;
				int i = 0;
				int n = original.Count;
				while (i < n)
				{
					Expression p = this.Visit(original[i]);
					if (list != null)
					{
						list.Add(p);
					}
					else if (p != original[i])
					{
						list = new List<Expression>(n);
						for (int j = 0; j < i; j++)
						{
							list.Add(original[j]);
						}
						list.Add(p);
					}
					i++;
				}
				if (list != null)
				{
					expressions = list.AsReadOnly();
					return expressions;
				}
			}
			expressions = original;
			return expressions;
		}

		protected virtual new Expression VisitInvocation(InvocationExpression iv)
		{
			IEnumerable<Expression> args = this.VisitExpressionList(iv.Arguments);
			Expression expr = this.Visit(iv.Expression);
			return this.UpdateInvocation(iv, expr, args);
		}

		protected virtual Expression VisitLambda(LambdaExpression lambda)
		{
			Expression body = this.Visit(lambda.Body);
			Expression expression = this.UpdateLambda(lambda, lambda.Type, body, lambda.Parameters);
			return expression;
		}

		protected virtual new Expression VisitListInit(ListInitExpression init)
		{
			NewExpression n = this.VisitNew(init.NewExpression);
			IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(init.Initializers);
			return this.UpdateListInit(init, n, initializers);
		}

		protected virtual Expression VisitMemberAccess(MemberExpression m)
		{
			Expression exp = this.Visit(m.Expression);
			return this.UpdateMemberAccess(m, exp, m.Member);
		}

		protected virtual Expression VisitMemberAndExpression(MemberInfo member, Expression expression)
		{
			return this.Visit(expression);
		}

		protected virtual ReadOnlyCollection<Expression> VisitMemberAndExpressionList(ReadOnlyCollection<MemberInfo> members, ReadOnlyCollection<Expression> original)
		{
			ReadOnlyCollection<Expression> expressions;
			MemberInfo item;
			if (original != null)
			{
				List<Expression> list = null;
				int i = 0;
				int n = original.Count;
				while (i < n)
				{
					if (members != null)
					{
						item = members[i];
					}
					else
					{
						item = null;
					}
					Expression p = this.VisitMemberAndExpression(item, original[i]);
					if (list != null)
					{
						list.Add(p);
					}
					else if (p != original[i])
					{
						list = new List<Expression>(n);
						for (int j = 0; j < i; j++)
						{
							list.Add(original[j]);
						}
						list.Add(p);
					}
					i++;
				}
				if (list != null)
				{
					expressions = list.AsReadOnly();
					return expressions;
				}
			}
			expressions = original;
			return expressions;
		}

		protected virtual new MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			Expression e = this.Visit(assignment.Expression);
			return this.UpdateMemberAssignment(assignment, assignment.Member, e);
		}

		protected virtual new Expression VisitMemberInit(MemberInitExpression init)
		{
			NewExpression n = this.VisitNew(init.NewExpression);
			IEnumerable<MemberBinding> bindings = this.VisitBindingList(init.Bindings);
			return this.UpdateMemberInit(init, n, bindings);
		}

		protected virtual new MemberListBinding VisitMemberListBinding(MemberListBinding binding)
		{
			IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(binding.Initializers);
			return this.UpdateMemberListBinding(binding, binding.Member, initializers);
		}

		protected virtual new MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
		{
			IEnumerable<MemberBinding> bindings = this.VisitBindingList(binding.Bindings);
			return this.UpdateMemberMemberBinding(binding, binding.Member, bindings);
		}

		protected virtual new Expression VisitMethodCall(MethodCallExpression m)
		{
			Expression obj = this.Visit(m.Object);
			IEnumerable<Expression> args = this.VisitExpressionList(m.Arguments);
			return this.UpdateMethodCall(m, obj, m.Method, args);
		}

		protected virtual new NewExpression VisitNew(NewExpression nex)
		{
			IEnumerable<Expression> args = this.VisitMemberAndExpressionList(nex.Members, nex.Arguments);
			NewExpression newExpression = this.UpdateNew(nex, nex.Constructor, args, nex.Members);
			return newExpression;
		}

		protected virtual new Expression VisitNewArray(NewArrayExpression na)
		{
			IEnumerable<Expression> exprs = this.VisitExpressionList(na.Expressions);
			return this.UpdateNewArray(na, na.Type, exprs);
		}

		protected virtual new Expression VisitParameter(ParameterExpression p)
		{
			return p;
		}

		protected virtual Expression VisitTypeIs(TypeBinaryExpression b)
		{
			Expression expr = this.Visit(b.Expression);
			return this.UpdateTypeIs(b, expr, b.TypeOperand);
		}

		protected virtual new Expression VisitUnary(UnaryExpression u)
		{
			Expression operand = this.Visit(u.Operand);
			Expression expression = this.UpdateUnary(u, operand, u.Type, u.Method);
			return expression;
		}

		protected virtual Expression VisitUnknown(Expression expression)
		{
			throw new Exception(string.Format("Unhandled expression type: '{0}'", expression.NodeType));
		}
	}
}