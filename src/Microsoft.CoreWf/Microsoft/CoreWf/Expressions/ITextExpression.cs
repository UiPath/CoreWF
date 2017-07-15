// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace CoreWf.Expressions
{
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

        bool RequiresCompilation
        {
            get;
        }

        Expression GetExpressionTree();
    }
}
