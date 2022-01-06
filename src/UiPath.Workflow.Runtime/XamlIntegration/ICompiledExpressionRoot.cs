// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace System.Activities.XamlIntegration;

public interface ICompiledExpressionRoot
{
    string GetLanguage();
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters, Justification = "Interface is intended to be implemented only by generated code and consumed only by internal code")]
    bool CanExecuteExpression(string expressionText, bool isReference, IList<LocationReference> locations, out int expressionId);
    bool CanExecuteExpression(Type type, string expressionText, bool isReference, IList<LocationReference> locations, out int expressionId) =>
        CanExecuteExpression(expressionText, isReference, locations, out expressionId);
    object InvokeExpression(int expressionId, IList<LocationReference> locations, ActivityContext activityContext);
    object InvokeExpression(int expressionId, IList<Location> locations);
    IList<string> GetRequiredLocations(int expressionId);
    Expression GetExpressionTreeForExpression(int expressionId, IList<LocationReference> locationReferences);
}