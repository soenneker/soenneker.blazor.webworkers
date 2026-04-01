using System;
using System.Linq.Expressions;
using Soenneker.Blazor.WebWorkers.Dtos;
using Soenneker.Extensions.String;

namespace Soenneker.Blazor.WebWorkers.Internals;

internal static class DotNetInvocationExpressionParser
{
    internal static WebWorkerRequest Parse(LambdaExpression expression, string? poolName = null)
    {
        ArgumentNullException.ThrowIfNull(expression);

        if (expression.Body is not MethodCallExpression methodCall)
            throw new ArgumentException("The expression must be a direct method call.", nameof(expression));

        if (!methodCall.Method.IsStatic)
            throw new ArgumentException("Only static methods can be executed in a web worker.", nameof(expression));

        if (methodCall.Method.DeclaringType == null || methodCall.Method.DeclaringType.FullName.IsNullOrWhiteSpace())
            throw new ArgumentException("The target method must have a declaring type with a full name.", nameof(expression));

        var arguments = new object[methodCall.Arguments.Count];

        for (var index = 0; index < methodCall.Arguments.Count; index++)
        {
            arguments[index] = EvaluateArgument(methodCall.Arguments[index])!;
        }

        return new WebWorkerRequest
        {
            Backend = Enums.WebWorkerBackend.DotNet,
            PoolName = poolName,
            RequestId = null,
            MethodName = $"{methodCall.Method.DeclaringType.FullName}.{methodCall.Method.Name}",
            Arguments = arguments
        };
    }

    private static object? EvaluateArgument(Expression expression)
    {
        UnaryExpression converted = Expression.Convert(expression, typeof(object));
        Expression<Func<object?>> lambda = Expression.Lambda<Func<object?>>(converted);
        return lambda.Compile().Invoke();
    }
}
