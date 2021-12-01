// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;

namespace System.Activities.Statements;

internal sealed class DefaultConfirmation : NativeActivity
{
    private readonly Activity _body;
    private readonly Variable<CompensationToken> _toConfirmToken;
    private CompletionCallback _onChildConfirmed;

    public DefaultConfirmation()
        : base()
    {
        _toConfirmToken = new Variable<CompensationToken>();

        _body = new InternalConfirm()
        {
            Target = new InArgument<CompensationToken>(_toConfirmToken),
        };
    }

    public InArgument<CompensationToken> Target { get; set; }

    private Activity Body => _body;

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        RuntimeArgument targetArgument = new RuntimeArgument("Target", typeof(CompensationToken), ArgumentDirection.In);
        metadata.Bind(Target, targetArgument);
        metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { targetArgument });

        metadata.SetImplementationVariablesCollection(new Collection<Variable> { _toConfirmToken });

        Fx.Assert(Body != null, "Body must be valid");
        metadata.SetImplementationChildrenCollection(new Collection<Activity> { Body });
    }

    protected override void Execute(NativeActivityContext context) => InternalExecute(context, null);

    private void InternalExecute(NativeActivityContext context, ActivityInstance completedInstance)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        if (compensationExtension == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ConfirmWithoutCompensableActivity(DisplayName)));
        }

        CompensationToken token = Target.Get(context);
        CompensationTokenData tokenData = token == null ? null : compensationExtension.Get(token.CompensationId);

        Fx.Assert(tokenData != null, "CompensationTokenData must be valid");

        if (tokenData.ExecutionTracker.Count > 0)
        {
            _onChildConfirmed ??= new CompletionCallback(InternalExecute);
            _toConfirmToken.Set(context, new CompensationToken(tokenData.ExecutionTracker.Get()));

            Fx.Assert(Body != null, "Body must be valid");
            context.ScheduleActivity(Body, _onChildConfirmed);
        }
    }

    protected override void Cancel(NativeActivityContext context)
    {
        // Suppress Cancel   
    }
}
