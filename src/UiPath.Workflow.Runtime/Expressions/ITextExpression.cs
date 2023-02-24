// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace System.Activities.Expressions;

public interface ITextExpression
{
    string ExpressionText
    {
        get;
    }

    string Language
    {
        get;
    }

    Expression GetExpressionTree();
}
