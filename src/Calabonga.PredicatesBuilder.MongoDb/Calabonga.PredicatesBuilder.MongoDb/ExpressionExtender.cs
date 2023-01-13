using System.Linq.Expressions;
using System.Reflection;

namespace Calabonga.PredicatesBuilder.MongoDb
{
    /// <summary>
    /// Custom expression visitor for ExpandableQuery. This expands calls to Expression.Compile() and
    /// collapses captured lambda references in sub queries which LINQ to SQL can't otherwise handle.
    /// </summary>
    class ExpressionExtender : ExpressionVisitor
    {
        // Replacement parameters - for when invoking a lambda expression.
        private readonly Dictionary<ParameterExpression, Expression>? _replaceVars;

        internal ExpressionExtender() { }

        private ExpressionExtender(Dictionary<ParameterExpression, Expression> replaceVars)
            => _replaceVars = replaceVars;

        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            if (_replaceVars != null && _replaceVars.ContainsKey(parameterExpression))
            {
                return _replaceVars[parameterExpression];
            }

            return base.VisitParameter(parameterExpression);
        }

        /// <summary>
        /// Flatten calls to Invoke so that Entity Framework can understand it. Calls to Invoke are generated
        /// by PredicateBuilder.
        /// </summary>
        protected override Expression VisitInvocation(InvocationExpression invocationExpression)
        {
            var target = invocationExpression.Expression;
            if (target is MemberExpression memberExpression)
            {
                target = TransformExpr(memberExpression);
            }

            if (target is ConstantExpression expression)
            {
                target = expression.Value as Expression;
            }

            var lambda = (LambdaExpression)target!;

            var replaceVars = _replaceVars == null
                ? new Dictionary<ParameterExpression, Expression>()
                : new Dictionary<ParameterExpression, Expression>(_replaceVars);

            try
            {
                for (var i = 0; i < lambda.Parameters.Count; i++)
                {
                    replaceVars.Add(lambda.Parameters[i], invocationExpression.Arguments[i]);
                }
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Invoke cannot be called recursively - try using a temporary variable.", ex);
            }

            return new ExpressionExtender(replaceVars).Visit(lambda.Body) ?? throw new InvalidOperationException();
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            switch (methodCallExpression.Method.Name)
            {
                case "Invoke" when methodCallExpression.Method.DeclaringType == typeof(QueryableExtensions):
                    {
                        var target = methodCallExpression.Arguments[0];
                        if (target is MemberExpression expression)
                        {
                            target = TransformExpr(expression);
                        }

                        if (target is ConstantExpression constantExpression)
                        {
                            target = constantExpression.Value as Expression;
                        }

                        var lambda = (LambdaExpression)target!;

                        var replaceVars = _replaceVars == null
                            ? new Dictionary<ParameterExpression, Expression>()
                            : new Dictionary<ParameterExpression, Expression>(_replaceVars);

                        try
                        {
                            for (var i = 0; i < lambda.Parameters.Count; i++)
                            {
                                replaceVars.Add(lambda.Parameters[i], methodCallExpression.Arguments[i + 1]);
                            }
                        }
                        catch (ArgumentException ex)
                        {
                            throw new InvalidOperationException("Invoke cannot be called recursively - try using a temporary variable.", ex);
                        }

                        return new ExpressionExtender(replaceVars).Visit(lambda.Body) ?? throw new InvalidOperationException();
                    }

                case "Compile" when methodCallExpression.Object is MemberExpression memberExpression:
                    {
                        var newExpr = TransformExpr(memberExpression);
                        if (newExpr != memberExpression)
                        {
                            return newExpr;
                        }

                        break;
                    }

                case "AsExpandable" when methodCallExpression.Method.DeclaringType == typeof(QueryableExtensions):
                    return methodCallExpression.Arguments[0];
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        protected override Expression VisitMemberAccess(MemberExpression memberExpression) =>
            memberExpression.Member.DeclaringType != null && memberExpression.Member.DeclaringType.Name.StartsWith("<>")
            ? TransformExpr(memberExpression)
            : base.VisitMemberAccess(memberExpression);

        Expression TransformExpr(MemberExpression input)
        {
            // Collapse captured outer variables
            if (input.Member.ReflectedType != null && (input is not { Member: FieldInfo }
                                                       || !input.Member.ReflectedType.IsNestedPrivate
                                                       || !input.Member.ReflectedType.Name.StartsWith("<>"))) // captured outer variable
            {
                return input;
            }

            if (input.Expression is not ConstantExpression expression)
            {
                return input;
            }

            var obj = expression.Value;
            if (obj == null)
            {
                return input;
            }

            var t = obj.GetType();
            if (!t.IsNestedPrivate || !t.Name.StartsWith("<>"))
            {
                return input;
            }

            var fi = (FieldInfo)input.Member;
            var result = fi.GetValue(obj);
            return (result is Expression exp
                ? Visit(exp)
                : input) ?? throw new InvalidOperationException();
        }
    }
}