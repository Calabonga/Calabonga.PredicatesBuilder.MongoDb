using System.Linq.Expressions;

namespace Calabonga.PredicatesBuilder.MongoDb
{
    /// <summary>
    /// Refer to http://www.albahari.com/nutshell/linqkit.html and
    /// http://tomasp.net/blog/linq-expand.aspx for more information.
    /// </summary>
    public static class QueryableExtensions
    {
        public static IQueryable<T> AsExtendable<T>(this IQueryable<T> query)
        {
            if (query is ExpandableQuery<T> expandableQuery)
            {
                return expandableQuery;
            }

            return new ExpandableQuery<T>(query);
        }

        public static Expression<TDelegate>? Extend<TDelegate>(this Expression<TDelegate> source)
        {
            return (Expression<TDelegate>?)new ExpressionExtender().Visit(source);
        }

        public static Expression? Extend(this Expression source)
        {
            return new ExpressionExtender().Visit(source);
        }

        public static TResult Invoke<TResult>(this Expression<Func<TResult>> source)
        {
            return source.Compile().Invoke();
        }

        public static TResult Invoke<T1, TResult>(this Expression<Func<T1, TResult>> source, T1 arg1)
        {
            return source.Compile().Invoke(arg1);
        }

        public static TResult Invoke<T1, T2, TResult>(this Expression<Func<T1, T2, TResult>> source, T1 arg1, T2 arg2)
        {
            return source.Compile().Invoke(arg1, arg2);
        }

        public static TResult Invoke<T1, T2, T3, TResult>(this Expression<Func<T1, T2, T3, TResult>> source, T1 arg1, T2 arg2, T3 arg3)
        {
            return source.Compile().Invoke(arg1, arg2, arg3);
        }

        public static TResult Invoke<T1, T2, T3, T4, TResult>(this Expression<Func<T1, T2, T3, T4, TResult>> source, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return source.Compile().Invoke(arg1, arg2, arg3, arg4);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
            {
                action(element);
            }
        }
    }
}