// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CoreWf.Validation
{
    public sealed class GetChildSubtree : CodeActivity<IEnumerable<Activity>>
    {
        public GetChildSubtree()
            : base()
        {
        }

        public InArgument<ValidationContext> ValidationContext
        {
            get;
            set;
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            RuntimeArgument runtimeArgument = new RuntimeArgument("ValidationContext", typeof(ValidationContext), ArgumentDirection.In, true);
            metadata.Bind(this.ValidationContext, runtimeArgument);

            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { runtimeArgument });
        }

        protected override IEnumerable<Activity> Execute(CodeActivityContext context)
        {
            Fx.Assert(this.ValidationContext != null, "ValidationContext must not be null");

            ValidationContext currentContext = this.ValidationContext.Get(context);
            if (currentContext != null)
            {
                return currentContext.GetChildren();
            }
            else
            {
                return ActivityValidationServices.EmptyChildren;
            }
        }
    }
}
