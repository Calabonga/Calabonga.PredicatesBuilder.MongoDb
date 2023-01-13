using System.Collections;
using System.Linq.Expressions;

namespace Calabonga.PredicatesBuilder.MongoDb
{
    /// <summary>
    /// An IQueryable wrapper that allows us to visit the query's expression tree just before LINQ to SQL gets to it.
    /// This is based on the excellent work of Tomas Petricek: http://tomasp.net/blog/linq-expand.aspx
    /// </summary>
    public class ExpandableQuery<T> : IQueryable<T>, IOrderedQueryable<T>, IOrderedQueryable
    {
        private readonly ExpandableQueryProvider<T> _provider;
        private readonly IQueryable<T> _innerQueryable;

        internal IQueryable<T> InnerQueryableQuery => _innerQueryable; // Original query, that we're wrapping

        internal ExpandableQuery(IQueryable<T> innerQueryable)
        {
            _innerQueryable = innerQueryable;
            _provider = new ExpandableQueryProvider<T>(this);
        }

        Expression IQueryable.Expression => _innerQueryable.Expression;
        Type IQueryable.ElementType => typeof(T);
        IQueryProvider IQueryable.Provider => _provider;

        public IEnumerator<T> GetEnumerator()
        {
            return _innerQueryable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerQueryable.GetEnumerator();
        }

        public override string? ToString()
        {
            return _innerQueryable.ToString();
        }
    }

    class ExpandableQueryProvider<T> : IQueryProvider
    {
        private readonly ExpandableQuery<T> _query;

        internal ExpandableQueryProvider(ExpandableQuery<T> query)
        {
            _query = query;
        }

        /// <summary>Constructs an <see cref="T:System.Linq.IQueryable`1" /> object that can evaluate the query represented by a specified expression tree.</summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <typeparam name="TElement">The type of the elements of the <see cref="T:System.Linq.IQueryable`1" /> that is returned.</typeparam>
        /// <returns>An <see cref="T:System.Linq.IQueryable`1" /> that can evaluate the query represented by the specified expression tree.</returns>
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            return new ExpandableQuery<TElement>(_query.InnerQueryableQuery.Provider.CreateQuery<TElement>(expression.Extend() ?? throw new InvalidOperationException()));
        }

        /// <summary>Constructs an <see cref="T:System.Linq.IQueryable" /> object that can evaluate the query represented by a specified expression tree.</summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>An <see cref="T:System.Linq.IQueryable" /> that can evaluate the query represented by the specified expression tree.</returns>
        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            return _query.InnerQueryableQuery.Provider.CreateQuery(expression.Extend() ?? throw new InvalidOperationException());
        }

        /// <summary>Executes the strongly-typed query represented by a specified expression tree.</summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
        /// <returns>The value that results from executing the specified query.</returns>
        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            return _query.InnerQueryableQuery.Provider.Execute<TResult>(expression.Extend() ?? throw new InvalidOperationException());
        }

        /// <summary>Executes the query represented by a specified expression tree.</summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        object? IQueryProvider.Execute(Expression expression)
        {
            return _query.InnerQueryableQuery.Provider.Execute(expression.Extend() ?? throw new InvalidOperationException());
        }
    }
}