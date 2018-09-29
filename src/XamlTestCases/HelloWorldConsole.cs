using CoreWf;
using System;
using System.Collections.Generic;
using System.Text;

namespace XamlTestCases
{
    public sealed class HelloWorldConsole : CodeActivity<bool>
    {
        // Define an activity input argument of type string
        public InArgument<string> Text { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override bool Execute(CodeActivityContext context)
        {
            bool argumented = false;
            string textValue = Text.Get(context);
            if (textValue != null & textValue != String.Empty)
            {
                Console.WriteLine("Hello World from HelloWorldConsole CodeActivity without InArguments");
            }
            else
            {
                Console.WriteLine(textValue + " from HelloWorldConsole CodeActivity with InArguments");
                argumented = true;
            }
            return argumented;
        }
    }
}
