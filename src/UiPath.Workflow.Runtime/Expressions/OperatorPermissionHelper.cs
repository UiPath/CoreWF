// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Activities.Expressions;

internal static class OperatorPermissionHelper
{
    // The function we are returning from here may be cached in a static during CacheMetadata. This means the function would be used by multiple
    // workflow definitions using the same activity. In this case when CacheMetadata
    // is called in the second thru Nth usage, we will use the same cached function. If the operator overload method is not public and
    // any of the invocations of the workflow definitions are done without ReflectionPermission(MemberAccess), we would be opening up a security hole because
    // the user should not have the ability to invoke the non-public operator overload, but we would be allowing it because we cached the method during a
    // CacheMetadata episode when the permission was granted.
    // So, if the operator method is NOT public, we need to insert a Demand for ReflectionPermission.MemberAccess
    // into the function we are generating so that each usage will ensure that the permission is granted before calling
    // the operator method. If the operator method is public, we don't need the demand.
    // We don't need to check the public visibility of the declaring type because the user must have already constructed a generic activity
    // that is parameterized by the declaring type (i.e. new Add<Foo,Foo,Foo>).
    [Fx.Tag.SecurityNote(Miscellaneous =
        "RequiresReview - Functions invoking non-public overloaded operators get cached in a static and thus could get invoked under different permission sets."
            + " Ensure that the function contains a demand for ReflectionPermission(MemberAccess) if the method is non-public.")]
    internal static Expression InjectReflectionPermissionIfNecessary(MethodInfo method, Expression expression)
    {
#if NET45
        if (method == null)
        {
            return expression;
        }

        if (method.IsPublic)
        {
            return expression;
        }
        else
        {
            ReflectionPermission reflectionMemberAccessPermission = new ReflectionPermission(ReflectionPermissionFlag.MemberAccess);
            Expression demandExpression = Expression.Call(Expression.Constant(reflectionMemberAccessPermission), "Demand", null, null);
            return Expression.Block(expression.Type, demandExpression, expression);
        } 
#else
        return expression;
#endif
    }
}
