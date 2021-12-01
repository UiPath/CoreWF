// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Validation;

public sealed class GetChildSubtree : CodeActivity<IEnumerable<Activity>> 
{
    public GetChildSubtree()
        : base() { }

    public InArgument<ValidationContext> ValidationContext { get; set; }
        
    protected override IEnumerable<Activity> Execute(CodeActivityContext context)
    {
        Fx.Assert(ValidationContext != null, "ValidationContext must not be null");

        ValidationContext currentContext = ValidationContext.Get(context);
        return currentContext != null ? currentContext.GetChildren() : ActivityValidationServices.EmptyChildren;
    }
}
