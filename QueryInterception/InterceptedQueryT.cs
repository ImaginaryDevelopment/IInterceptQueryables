using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace QueryInterception
{
    internal class InterceptedQuery<T> : IOrderedQueryable<T>, IQueryable<T>, IEnumerable<T>, IOrderedQueryable, IQueryable, IEnumerable
    {

        protected internal readonly InterceptingProvider _provider;

        public Type ElementType => typeof(T);

        public Expression Expression { get; }


        public IQueryProvider Provider => _provider;

        internal InterceptedQuery(InterceptingProvider provider, Expression expression)
        {
            this._provider = provider;
            this.Expression = expression;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this._provider.ExecuteQuery<T>(this.Expression);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._provider.ExecuteQuery<T>(this.Expression);
        }
    }
}