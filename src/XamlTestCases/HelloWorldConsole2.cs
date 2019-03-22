using System;
using System.Activities;

namespace XamlTestCases
{
    public class HelloWorld2Input
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    public sealed class HelloWorldConsole2 : CodeActivity<bool>
    {
        // Define an activity input argument of type string
        public InArgument<HelloWorld2Input> Input { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override bool Execute(CodeActivityContext context)
        {
            bool withArguments = false;
            var inputValue = Input.Get(context);
            if (inputValue == null)
            {
                Console.WriteLine("Hello World from HelloWorldConsole2 CodeActivity without InArguments");
            }
            else
            {
                Console.WriteLine("Hello " + inputValue.FirstName + " " + inputValue.LastName + " from HelloWorldConsole2 CodeActivity with InArguments");
                withArguments = true;
            }
            return withArguments;
        }
    }
}
