// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

public abstract class ActivityWithResult : Activity
{
    internal ActivityWithResult() : base() { }

    public Type ResultType => InternalResultType;

    [IgnoreDataMember] // this member is repeated by all subclasses, which we control
    public OutArgument Result
    {
        get => ResultCore;
        set => ResultCore = value;
    }

    internal abstract Type InternalResultType { get; }

    internal abstract OutArgument ResultCore { get; set; }

    internal RuntimeArgument ResultRuntimeArgument { get; set; }

    internal abstract object InternalExecuteInResolutionContextUntyped(CodeActivityContext resolutionContext);

    internal override bool IsActivityWithResult => true;
}
