using System;
using System.Activities;

namespace XamlTestCases
{
    public class PersonToGreet
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public sealed class ActivityWithObjectArgument : CodeActivity<bool>
    {
        // Define an activity input argument of type string
        public InArgument<PersonToGreet> Input { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override bool Execute(CodeActivityContext context)
        {
            bool withArguments = false;
            var inputValue = Input.Get(context);
            if (inputValue == null)
            {
                Console.WriteLine("Hello World from ActivityWithObjectArgument");
            }
            else
            {
                Console.WriteLine("Hello " + inputValue.FirstName + " " + inputValue.LastName + " from ActivityWithObjectArgument");
                withArguments = true;
            }
            return withArguments;
        }
    }
}
