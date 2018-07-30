// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf;
using System.Collections.ObjectModel;

namespace Test.Common.TestObjects.CustomActivities
{
    public class Increment : CodeActivity
    {
        private InOutArgument<int> _counter;
        private InArgument<int> _incrementCount;

        public Increment()
            : this(1)
        {
        }

        public Increment(int incrementCount)
        {
            this.IncrementCount = new InArgument<int>(incrementCount);
        }

        public InArgument<int> IncrementCount
        {
            get
            {
                return _incrementCount;
            }
            set
            {
                _incrementCount = value;
            }
        }

        public InOutArgument<int> Counter
        {
            get
            {
                return _counter;
            }
            set
            {
                _counter = value;
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("IncrementCount", typeof(int), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Counter", typeof(int), ArgumentDirection.InOut));
            metadata.Bind(this.IncrementCount, runtimeArguments[0]);
            metadata.Bind(this.Counter, runtimeArguments[1]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(CodeActivityContext context)
        {
            this.Counter.Set(
                context,
                this.Counter.Get(context) + this.IncrementCount.Get(context)
                );
        }
    }
}
