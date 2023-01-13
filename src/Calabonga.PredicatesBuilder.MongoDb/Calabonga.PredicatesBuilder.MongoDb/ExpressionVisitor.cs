using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Calabonga.PredicatesBuilder.MongoDb
{
    /// <summary>
    /// This comes from Matt Warren's sample:
    /// http://blogs.msdn.com/mattwar/archive/2007/07/31/linq-building-an-iqueryable-provider-part-ii.aspx
    /// </summary>
    public abstract class ExpressionVisitor
    {
        public virtual Expression? Visit(Expression? exp)
        {
            if (exp == null)
            {
                return exp;
            }

            switch (exp.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return VisitUnary((UnaryExpression)exp);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return VisitBinary((BinaryExpression)exp);
                case ExpressionType.TypeIs:
                    return VisitTypeIs((TypeBinaryExpression)exp);
                case ExpressionType.Conditional:
                    return VisitConditional((ConditionalExpression)exp);
                case ExpressionType.Constant:
                    return VisitConstant((ConstantExpression)exp);
                case ExpressionType.Parameter:
                    return VisitParameter((ParameterExpression)exp);
                case ExpressionType.MemberAccess:
                    return VisitMemberAccess((MemberExpression)exp);
                case ExpressionType.Call:
                    return VisitMethodCall((MethodCallExpression)exp);
                case ExpressionType.Lambda:
                    return VisitLambda((LambdaExpression)exp);
                case ExpressionType.New:
                    return VisitNew((NewExpression)exp);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return VisitNewArray((NewArrayExpression)exp);
                case ExpressionType.Invoke:
                    return VisitInvocation((InvocationExpression)exp);
                case ExpressionType.MemberInit:
                    return VisitMemberInit((MemberInitExpression)exp);
                case ExpressionType.ListInit:
                    return VisitListInit((ListInitExpression)exp);
                case ExpressionType.UnaryPlus:
                case ExpressionType.Power:
                case ExpressionType.Assign:
                case ExpressionType.Block:
                case ExpressionType.DebugInfo:
                case ExpressionType.Decrement:
                case ExpressionType.Dynamic:
                case ExpressionType.Default:
                case ExpressionType.Extension:
                case ExpressionType.Goto:
                case ExpressionType.Increment:
                case ExpressionType.Index:
                case ExpressionType.Label:
                case ExpressionType.RuntimeVariables:
                case ExpressionType.Loop:
                case ExpressionType.Switch:
                case ExpressionType.Throw:
                case ExpressionType.Try:
                case ExpressionType.Unbox:
                case ExpressionType.AddAssign:
                case ExpressionType.AndAssign:
                case ExpressionType.DivideAssign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.OrAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.SubtractAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PostDecrementAssign:
                case ExpressionType.TypeEqual:
                case ExpressionType.OnesComplement:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                default:
                    throw new Exception($"Unhandled expression type: '{exp.NodeType}'");
            }
        }

        protected virtual MemberBinding VisitBinding(MemberBinding binding)
        {
            return binding.BindingType switch
            {
                MemberBindingType.Assignment => VisitMemberAssignment((MemberAssignment)binding),
                MemberBindingType.MemberBinding => VisitMemberMemberBinding((MemberMemberBinding)binding),
                MemberBindingType.ListBinding => VisitMemberListBinding((MemberListBinding)binding),
                _ => throw new Exception($"Unhandled binding type '{binding.BindingType}'")
            };
        }

        protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
        {
            var arguments = VisitExpressionList(initializer.Arguments);
            return arguments != initializer.Arguments
                ? Expression.ElementInit(initializer.AddMethod, arguments)
                : initializer;
        }

        protected virtual Expression VisitUnary(UnaryExpression unaryExpression)
        {
            var operand = Visit(unaryExpression.Operand);
            return operand != unaryExpression.Operand
                ? Expression.MakeUnary(unaryExpression.NodeType, operand!, unaryExpression.Type, unaryExpression.Method)
                : unaryExpression;
        }

        protected virtual Expression VisitBinary(BinaryExpression binaryExpression)
        {
            var left = Visit(binaryExpression.Left);
            var right = Visit(binaryExpression.Right);
            var conversion = Visit(binaryExpression.Conversion);
            if (left == binaryExpression.Left && right == binaryExpression.Right &&
                conversion == binaryExpression.Conversion)
            {
                return binaryExpression;
            }

            return binaryExpression is { NodeType: ExpressionType.Coalesce, Conversion: { } }
                ? Expression.Coalesce(left!, right!, conversion as LambdaExpression)
                : Expression.MakeBinary(binaryExpression.NodeType, left!, right!, binaryExpression.IsLiftedToNull,
                    binaryExpression.Method);
        }

        protected virtual Expression VisitTypeIs(TypeBinaryExpression typeBinaryExpression)
        {
            var expr = Visit(typeBinaryExpression.Expression);
            return expr != typeBinaryExpression.Expression
                ? Expression.TypeIs(expr!, typeBinaryExpression.TypeOperand)
                : typeBinaryExpression;
        }

        protected virtual Expression VisitConstant(ConstantExpression constantExpression)
        {
            return constantExpression;
        }

        protected virtual Expression VisitConditional(ConditionalExpression conditionalExpression)
        {
            var test = Visit(conditionalExpression.Test);
            var ifTrue = Visit(conditionalExpression.IfTrue);
            var ifFalse = Visit(conditionalExpression.IfFalse);
            if (test != conditionalExpression.Test || ifTrue != conditionalExpression.IfTrue ||
                ifFalse != conditionalExpression.IfFalse)
            {
                return Expression.Condition(test!, ifTrue!, ifFalse!);
            }

            return conditionalExpression;
        }

        protected virtual Expression VisitParameter(ParameterExpression parameterExpression)
        {
            return parameterExpression;
        }

        protected virtual Expression VisitMemberAccess(MemberExpression memberExpression)
        {
            var exp = Visit(memberExpression.Expression);
            return exp != memberExpression.Expression
                ? Expression.MakeMemberAccess(exp, memberExpression.Member)
                : memberExpression;
        }

        protected virtual Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            var obj = Visit(methodCallExpression.Object);
            IEnumerable<Expression> args = VisitExpressionList(methodCallExpression.Arguments);
            if (obj != methodCallExpression.Object || args != methodCallExpression.Arguments)
            {
                return Expression.Call(obj, methodCallExpression.Method, args);
            }

            return methodCallExpression;
        }

        protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            List<Expression>? list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                var p = Visit(original[i]);
                if (list != null)
                {
                    if (p != null)
                    {
                        list.Add(p);
                    }
                }
                else if (p != original[i])
                {
                    list = new List<Expression>(n);
                    for (var j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }

                    if (p != null)
                    {
                        list.Add(p);
                    }
                }
            }

            return list != null
                ? list.AsReadOnly()
                : original;
        }

        protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment memberAssignment)
        {
            var expression = Visit(memberAssignment.Expression);
            return expression != memberAssignment.Expression
                ? Expression.Bind(memberAssignment.Member, expression!)
                : memberAssignment;
        }

        protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding memberMemberBinding)
        {
            var bindings = VisitBindingList(memberMemberBinding.Bindings);
            return bindings != memberMemberBinding.Bindings
                ? Expression.MemberBind(memberMemberBinding.Member, bindings)
                : memberMemberBinding;
        }

        protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding memberListBinding)
        {
            var initializers = VisitElementInitializerList(memberListBinding.Initializers);
            return initializers != memberListBinding.Initializers
                ? Expression.ListBind(memberListBinding.Member, initializers)
                : memberListBinding;
        }

        protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            List<MemberBinding>? list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                var b = VisitBinding(original[i]);
                if (list != null)
                {
                    list.Add(b);
                }
                else if (b != original[i])
                {
                    list = new List<MemberBinding>(n);
                    for (var j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }

                    list.Add(b);
                }
            }

            if (list != null)
            {
                return list;
            }

            return original;
        }

        protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
        {
            List<ElementInit>? list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                var init = VisitElementInitializer(original[i]);
                if (list != null)
                {
                    list.Add(init);
                }
                else if (init != original[i])
                {
                    list = new List<ElementInit>(n);
                    for (var j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }

                    list.Add(init);
                }
            }

            if (list != null)
            {
                return list;
            }

            return original;
        }

        protected virtual Expression VisitLambda(LambdaExpression lambdaExpression)
        {
            var body = Visit(lambdaExpression.Body);
            return body != lambdaExpression.Body
                ? Expression.Lambda(lambdaExpression.Type, body!, lambdaExpression.Parameters)
                : lambdaExpression;
        }

        protected virtual NewExpression VisitNew(NewExpression newExpression)
        {
            IEnumerable<Expression> args = VisitExpressionList(newExpression.Arguments);
            if (Equals(args, newExpression.Arguments))
            {
                return newExpression;
            }

            return newExpression.Members != null
                ? Expression.New(newExpression.Constructor!, args, newExpression.Members)
                : Expression.New(newExpression.Constructor!, args);
        }

        protected virtual Expression VisitMemberInit(MemberInitExpression memberInitExpression)
        {
            var n = VisitNew(memberInitExpression.NewExpression);
            var bindings = VisitBindingList(memberInitExpression.Bindings);
            if (n != memberInitExpression.NewExpression || !Equals(bindings, memberInitExpression.Bindings))
            {
                return Expression.MemberInit(n, bindings);
            }

            return memberInitExpression;
        }

        protected virtual Expression VisitListInit(ListInitExpression listInitExpression)
        {
            var n = VisitNew(listInitExpression.NewExpression);
            var initializers = VisitElementInitializerList(listInitExpression.Initializers);
            if (n != listInitExpression.NewExpression || !Equals(initializers, listInitExpression.Initializers))
            {
                return Expression.ListInit(n, initializers);
            }

            return listInitExpression;
        }

        protected virtual Expression VisitNewArray(NewArrayExpression newArrayExpression)
        {
            IEnumerable<Expression> expressions = VisitExpressionList(newArrayExpression.Expressions);
            if (expressions == newArrayExpression.Expressions)
            {
                return newArrayExpression;
            }

            return newArrayExpression.NodeType == ExpressionType.NewArrayInit
                ? Expression.NewArrayInit(newArrayExpression.Type.GetElementType()!, expressions)
                : Expression.NewArrayBounds(newArrayExpression.Type.GetElementType()!, expressions);

        }

        protected virtual Expression VisitInvocation(InvocationExpression invocationExpression)
        {
            IEnumerable<Expression> args = VisitExpressionList(invocationExpression.Arguments);
            var expr = Visit(invocationExpression.Expression);
            if (!Equals(args, invocationExpression.Arguments) || expr != invocationExpression.Expression)
            {
                return Expression.Invoke(expr!, args);
            }

            return invocationExpression;
        }
    }
}