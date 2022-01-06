// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Exception")]
//[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotMatchKeywords, Justification = "Optimizing for XAML naming. VB imperative users will [] qualify (e.g. New [Throw])")]
public sealed class Throw : CodeActivity
{
    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<Exception> Exception { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        RuntimeArgument exceptionArgument = new("Exception", typeof(Exception), ArgumentDirection.In, true);
        metadata.Bind(Exception, exceptionArgument);
        metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { exceptionArgument });
    }

    protected override void Execute(CodeActivityContext context)
    {
        Exception exception = Exception.Get(context);

        if (exception == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.MemberCannotBeNull("Exception", GetType().Name, DisplayName)));
        }

        throw FxTrace.Exception.AsError(exception);
    }
}
