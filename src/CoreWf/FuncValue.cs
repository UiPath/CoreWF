// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

public class FuncValue<TResult> : CodeActivity<TResult>
{
    private readonly Func<ActivityContext, TResult> _func;
    public FuncValue(Func<ActivityContext, TResult> func) => _func = func ?? throw new ArgumentNullException(nameof(func));
    protected override TResult Execute(CodeActivityContext context)
    {
        try
        {
            context.AllowChainedEnvironmentAccess = true;
            return _func(context);
        }
        finally
        {
            context.AllowChainedEnvironmentAccess = false;
        }
    }
}
