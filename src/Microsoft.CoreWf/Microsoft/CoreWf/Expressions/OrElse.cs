// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Statements;
using System.ComponentModel;

namespace Microsoft.CoreWf.Expressions
{
    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotMatchKeywords, //Justification = "Optimizing for XAML naming. VB imperative users will [] qualify (e.g. New [OrElse])")]
    public sealed class OrElse : Activity<bool>
    {
        public OrElse()
            : base()
        {
            this.Implementation =
                () =>
                {
                    if (this.Left != null && this.Right != null)
                    {
                        return new If
                        {
                            Condition = this.Left,
                            Then = new Assign<bool>
                            {
                                To = new OutArgument<bool>(context => this.Result.Get(context)),
                                Value = true,
                            },
                            Else = new Assign<bool>
                            {
                                To = new OutArgument<bool>(context => this.Result.Get(context)),
                                Value = new InArgument<bool>(this.Right)
                            }
                        };
                    }
                    else
                    {
                        return null;
                    }
                };
        }

        [DefaultValue(null)]
        public Activity<bool> Left
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public Activity<bool> Right
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(UpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(ActivityMetadata metadata)
        {
            metadata.AddImportedChild(this.Left);
            metadata.AddImportedChild(this.Right);

            if (this.Left == null)
            {
                metadata.AddValidationError(SR.BinaryExpressionActivityRequiresArgument("Left", "OrElse", this.DisplayName));
            }

            if (this.Right == null)
            {
                metadata.AddValidationError(SR.BinaryExpressionActivityRequiresArgument("Right", "OrElse", this.DisplayName));
            }
        }
    }
}
