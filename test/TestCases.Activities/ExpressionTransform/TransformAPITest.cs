// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using System.Linq.Expressions;
using Test.Common.TestObjects.Activities.ExpressionTransform;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities.ExpressionTransform
{
    public class TransformAPITest
    {
        /// <summary>
        /// Pass null to Convert. ValidationException is expected
        /// </summary>        
        [Fact]
        public void ConvertNull()
        {
            ArgumentNullException expectedException = new ArgumentNullException("expression", ErrorStrings.ExpressionRequiredForConversion);

            Expression<Func<ActivityContext, int>> expression = null;
            ExpressionTestRuntime.Convert(expression, expectedException);
        }

        /// <summary>
        /// Pass null to TryConvert. Should return false and output null result.
        /// </summary>        
        [Fact]
        public void TryConvertNull()
        {
            Expression<Func<ActivityContext, int>> expression = null;
            ExpressionTestRuntime.TryConvert(expression, false);
        }

        /// <summary>
        /// Pass null to ConvertReference. ValidationException is expected.
        /// </summary>        
        [Fact]
        public void ConvertReferenceNull()
        {
            ArgumentNullException expectedException = new ArgumentNullException("expression", ErrorStrings.ExpressionRequiredForConversion);

            Expression<Func<ActivityContext, int>> expression = null;
            ExpressionTestRuntime.ConvertReference(expression, expectedException);
        }

        /// <summary>
        /// Pass null TryConvertReference. Return false and output null result.
        /// </summary>        
        [Fact]
        public void TryConvertReferenceNull()
        {
            Expression<Func<ActivityContext, int>> expression = null;
            ExpressionTestRuntime.TryConvertReference(expression, false);
        }
    }
}
