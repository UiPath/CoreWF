using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using Xunit;

namespace TestCases.Workflows
{
    public class ValidationOptimizationTests
    {

        [Fact]
        public void ValidationWithSkipImplementation()
        {
            //Even if there should be a compilation error, validation with SkipImplementationChildren should not show it
            var wf = new Sequence
            {
                Activities =
                {
                    new LogMessage()
                    {
                        privateMessage = new InArgument<string>("bla")
                    }
                }
            };

            var result = ActivityValidationServices.Validate(wf, new ValidationSettings
            {
                ForceExpressionCache = false,
                SkipImplementationChildren = true,
            });
            result.Errors.Count.ShouldBe(0);

            var result2 = ActivityValidationServices.Validate(wf, new ValidationSettings
            {
                ForceExpressionCache = false,
                SkipImplementationChildren = false,
            });
            result2.Errors.Count.ShouldBe(1);

        }

        [Fact]
        public void ValidationWithSkipImplementation_SkipFalse()
        {
            //Normal use case (SkipImplementationChildren = false) where the activity is validated, and error is shown
            var wf = new Sequence
            {
                Activities =
                {
                    new LogMessage()
                    { 
                        privateMessage = new InArgument<string>("bla")
                    }
                }
            };


            var result = ActivityValidationServices.Validate(wf, new ValidationSettings
            {
                ForceExpressionCache = false,
                SkipImplementationChildren = false,
            });
            result.Errors.Count.ShouldBe(1);
        }

        [Fact]
        public void ValidationWithSkipImplementation_DynamicActivityWorks()
        {
            //Tests that a activities in a dynamic activity are validated, and not skipped.
            var wf = new Sequence
            {
                Activities =
                {
                    new LogMessage()
                    {
                        privateMessage= new InArgument<string>(new VisualBasicValue<string>("inexistentVariable")),
                    }
                }
            };


            var dynamicActivity = new DynamicActivity
            {
                Implementation = () => wf
            };


            var result = ActivityValidationServices.Validate(dynamicActivity, new ValidationSettings
            {
                ForceExpressionCache = false,
                SkipImplementationChildren = true,
            });
            result.Errors.Count.ShouldBe(1);
        }
    }




    [Browsable(true)]
    [Category("SimpleLibrary")]
    [DisplayName("LogMessage")]
    [Description("UPTF000000B4eyI8SGVscExpbms+a19fQmFja2luZ0ZpZWxkIjpudWxsLCI8SW5pdGlhbFRvb2x0aXA+a19fQmFja2luZ0ZpZWxkIjpudWxsLCI8VG9vbHRpcD5rX19CYWNraW5nRmllbGQiOm51bGwsIjxWZXJzaW9uPmtfX0JhY2tpbmdGaWVsZCI6MX0=")]
    public class LogMessage : Activity
    {

        [Category("Input")]
        public InArgument<string> privateMessage { get; set; }

        internal Dictionary<object, string> ConstructorIdRefDictionary { get; set; }

        internal Dictionary<object, string> IdRefDictionary { get; set; }

        public LogMessage()
        {

            ConstructorIdRefDictionary = new Dictionary<object, string>();
            this.Implementation = GetImplementation;
        }

        private Activity GetImplementation()
        {
            IdRefDictionary = new Dictionary<object, string>(ConstructorIdRefDictionary);

            Sequence sequence = new Sequence{ DisplayName = "Sequence"};
            Collection<Activity> activities = sequence.Activities;

            WriteLine invalidWriteLine = new WriteLine
            {
                Text = new InArgument<string>(new VisualBasicValue<string>("inexistentVariable")),
                DisplayName = "Log Message - not compiling"
            };
            activities.Add(invalidWriteLine);


            return sequence;
        }
    }

}
