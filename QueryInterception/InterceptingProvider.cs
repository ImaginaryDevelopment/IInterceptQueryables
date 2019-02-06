using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Objects;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace QueryInterception
{
    public class InterceptingProvider : IQueryProvider
    {
        private readonly IQueryProvider _underlyingProvider;

        private readonly Func<Expression, Expression>[] _visitors;

        private readonly Func<Expression, Expression> _afterUnderlyingVisitor;

        public static bool DoTrace
        {
            get;
            set;
        }

        private InterceptingProvider(Func<Expression, Expression> afterUnderlyingVisitor, IQueryProvider underlyingQueryProvider, params Func<Expression, Expression>[] visitors)
        {
            this._underlyingProvider = underlyingQueryProvider;
            this._afterUnderlyingVisitor = afterUnderlyingVisitor;
            this._visitors = visitors;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new InterceptedQuery<TElement>(this, expression);
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Type et = QueryInterception.TypeHelper.FindIEnumerable(expression.Type);
            Type type = typeof(InterceptedQuery<>);
            Type[] typeArray = new Type[] { et };
            Type qt = type.MakeGenericType(typeArray);
            object[] args = new object[] { this, expression };
            typeArray = new Type[] { typeof(InterceptingProvider), typeof(Expression) };
            ConstructorInfo ci = qt.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, typeArray, null);
            return (IQueryable)ci.Invoke(args);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            TResult tResult;
            Expression intercepted = this.InterceptExpr(expression);
            object result = this._underlyingProvider.Execute(intercepted);
            tResult = (result != null ? this.TranslateAnonymous<TResult>(result.GetType(), result) : default(TResult));
            return tResult;
        }

        public object Execute(Expression expression)
        {
            return this._underlyingProvider.Execute(this.InterceptExpr(expression));
        }

        public IEnumerator<TElement> ExecuteQuery<TElement>(Expression expression)
        {
            Expression intercepted;
            IQueryable newExpression;
            IEnumerator<TElement> enumerator1;
            IDisposable step = Profiler.Step("intercepting query");
            try
            {
                intercepted = this.InterceptExpr(expression);
            }
            finally
            {
                if (step != null)
                {
                    step.Dispose();
                }
            }
            step = Profiler.Step("Ef Translating query");
            try
            {
                newExpression = this._underlyingProvider.CreateQuery(intercepted);
            }
            finally
            {
                if (step != null)
                {
                    step.Dispose();
                }
            }
            Debug.Assert(!intercepted.Type.FullName.Contains("Shared"));
            if (InterceptingProvider.DoTrace)
            {
                step = Profiler.Step("ToTraceString");
                try
                {
                    Trace.WriteLine(((ObjectQuery)newExpression).ToTraceString());
                }
                finally
                {
                    if (step != null)
                    {
                        step.Dispose();
                    }
                }
            }
            if (this._afterUnderlyingVisitor != null)
            {
                this._afterUnderlyingVisitor(newExpression.Expression);
            }
            step = Profiler.Step("enumerating query");
            try
            {
                IEnumerator enumerator = newExpression.GetEnumerator();
                Type enumeratorType = enumerator.GetType();
                Type sourceArgumentType = enumeratorType.GetGenericArguments().Single<Type>();
                Type targetType = typeof(TElement);
                if (typeof(IEnumerator<TElement>).IsAssignableFrom(enumeratorType))
                {
                    enumerator1 = (IEnumerator<TElement>)enumerator;
                }
                else if (!targetType.IsAssignableFrom(sourceArgumentType))
                {
                    ConstructorInfo targetConstructor = targetType.GetConstructor(targetType.GetGenericArguments());
                    PropertyInfo[] properties = targetType.GetProperties();
                    PropertyInfo[] propertyInfoArray = sourceArgumentType.GetProperties();
                    Func<PropertyInfo, string> name = (PropertyInfo tp) => tp.Name;
                    Func<PropertyInfo, string> func = (PropertyInfo ep) => ep.Name;
                    IEnumerable<MethodInfo> eProperties = ((IEnumerable<PropertyInfo>)properties).Join<PropertyInfo, PropertyInfo, string, MethodInfo>((IEnumerable<PropertyInfo>)propertyInfoArray, name, func, (PropertyInfo tp, PropertyInfo ep) => ep.GetGetMethod());
                    List<TElement> items2 = new List<TElement>();
                    while (enumerator.MoveNext())
                    {
                        object current = enumerator.Current;
                        IEnumerable<object> targetParams =
                            from s in eProperties
                            select s.Invoke(current, null);
                        object newItem = targetConstructor.Invoke(targetParams.ToArray<object>());
                        items2.Add((TElement)newItem);
                    }
                    if (enumerator is IDisposable)
                    {
                        (enumerator as IDisposable).Dispose();
                    }
                    enumerator1 = items2.GetEnumerator();
                }
                else
                {
                    List<TElement> items = new List<TElement>();
                    while (enumerator.MoveNext())
                    {
                        items.Add((TElement)enumerator.Current);
                    }
                    if (enumerator is IDisposable)
                    {
                        (enumerator as IDisposable).Dispose();
                    }
                    enumerator1 = items.GetEnumerator();
                }
            }
            finally
            {
                if (step != null)
                {
                    step.Dispose();
                }
            }
            return enumerator1;
        }

        public static IQueryable<T> Intercept<T>(ExpressionVisitor afterUnderlyingVisitor, IQueryable<T> underlyingQuery, params ExpressionVisitor[] visitors)
        {
            Func<Expression, Expression> func;
            Func<Expression, Expression>[] visitFuncs = visitors.Select<ExpressionVisitor, Func<Expression, Expression>>((ExpressionVisitor v) =>
            {
                ExpressionVisitor expressionVisitor = v;
                return new Func<Expression, Expression>(expressionVisitor.Visit);
            }).ToArray<Func<Expression, Expression>>();
            if (afterUnderlyingVisitor != null)
            {
                ExpressionVisitor expressionVisitor1 = afterUnderlyingVisitor;
                func = new Func<Expression, Expression>(expressionVisitor1.Visit);
            }
            else
            {
                func = null;
            }
            return InterceptingProvider.Intercept<T>(func, underlyingQuery, visitFuncs);
        }

        public static IQueryable<T> Intercept<T>(IQueryable<T> underlyingQuery, params ExpressionVisitor[] visitors)
        {
            Func<Expression, Expression>[] visitFuncs = visitors.Select<ExpressionVisitor, Func<Expression, Expression>>((ExpressionVisitor v) =>
            {
                ExpressionVisitor expressionVisitor = v;
                return new Func<Expression, Expression>(expressionVisitor.Visit);
            }).ToArray<Func<Expression, Expression>>();
            return InterceptingProvider.Intercept<T>(null, underlyingQuery, visitFuncs);
        }

        public static IQueryable<T> Intercept<T>(Func<Expression, Expression> afterUnderlyingVisitor, IQueryable<T> underlyingQuery, params Func<Expression, Expression>[] visitors)
        {
            InterceptingProvider provider = new InterceptingProvider(afterUnderlyingVisitor, underlyingQuery.Provider, visitors);
            return provider.CreateQuery<T>(underlyingQuery.Expression);
        }

        private Expression InterceptExpr(Expression expression)
        {
            Expression exp = expression;
            Func<Expression, Expression>[] funcArray = this._visitors;
            for (int i = 0; i < funcArray.Length; i++)
            {
                exp = funcArray[i](exp);
            }
            return exp;
        }

        private TResult TranslateAnonymous<TResult>(Type inputType, object source)
        {
            TResult tResult;
            Type targetType = typeof(TResult);
            if (!targetType.IsAssignableFrom(inputType))
            {
                ConstructorInfo targetConstructor = targetType.GetConstructor(targetType.GetGenericArguments());
                IEnumerable<MethodInfo> eProperties =
                    from tp in targetType.GetProperties()
                    join ep in inputType.GetProperties() on tp.Name equals ep.Name
                    select ep.GetGetMethod();
                IEnumerable<object> targetParams =
                    from s in eProperties
                    select s.Invoke(source, null);
                tResult = (TResult)targetConstructor.Invoke(targetParams.ToArray());
            }
            else
            {
                tResult = (TResult)source;
            }
            return tResult;
        }
    }
}