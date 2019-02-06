using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace QueryInterception
{
	internal class InterceptedQuery<T> : IOrderedQueryable<T>, IQueryable<T>, IEnumerable<T>, IOrderedQueryable, IQueryable, IEnumerable
	{
		private System.Linq.Expressions.Expression _expression;

		protected internal readonly InterceptingProvider _provider;

		public Type ElementType
		{
			get
			{
				return typeof(T);
			}
		}

		public System.Linq.Expressions.Expression Expression
		{
			get
			{
				return this._expression;
			}
		}

		public IQueryProvider Provider
		{
			get
			{
				return this._provider;
			}
		}

		internal InterceptedQuery(InterceptingProvider provider, System.Linq.Expressions.Expression expression)
		{
			this._provider = provider;
			this._expression = expression;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this._provider.ExecuteQuery<T>(this._expression);
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this._provider.ExecuteQuery<T>(this._expression);
		}
	}
}