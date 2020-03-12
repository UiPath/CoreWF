using Shouldly;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using System.IO;
using System.Text;
using System.Threading;
using System.Xaml;
using Xunit;

namespace TestCases.Workflows.WF4Samples
{
    /// <summary>
    /// These tests are taken from the 
    /// <a href="https://social.technet.microsoft.com/wiki/contents/articles/12326.windows-workflow-wf-4-x-samples.aspx">WF 4.0 Samples</a>
    /// under Basic/Expressions
    /// </summary>
    public class Expressions
    {
        private const string CorrectOutput = @"John Doe earns $55000.00
Frank Kimono earns $89000.00
Salary statistics: minimum salary is $55000.00, maximum salary is $89000.00, average salary is $72000.00
";

        [Fact]
        public void ActivityXamlServicesLoad()
        {
            var xamlStream = this.GetType().Assembly.GetManifestResourceStream(this.GetType().Namespace + ".SalaryCalculation.xaml");
            var activity = ActivityXamlServices.Load(xamlStream);
            InvokeWorkflow(activity).ShouldBe(CorrectOutput);
        }

        [Fact]
        public void Code()
        {
            var activity = CreateCodeOnlyWorkflow();
            InvokeWorkflow(activity).ShouldBe(CorrectOutput);
        }

        [Fact]
        public void CodeToXaml()
        {
            var activity = CreateXamlSerializableCodeWorkflow();
            string workflowXamlString = XamlServices.Save(activity);
            activity = (Activity)XamlServices.Load(new StringReader(workflowXamlString));
            InvokeWorkflow(activity).ShouldBe(CorrectOutput);
        }

        private string InvokeWorkflow(Activity activity)
        {
            var stringBuilder = new StringBuilder();
            var consoleOutputWriter = new StringWriter(stringBuilder);
            AutoResetEvent are = new AutoResetEvent(false);
            var workflowApp = new WorkflowApplication(activity);
            workflowApp.Extensions.Add((TextWriter)consoleOutputWriter);
            workflowApp.Run();
            workflowApp.Completed = e =>
            {
                e.CompletionState.ShouldBe(ActivityInstanceState.Closed);
                are.Set();
            };
            workflowApp.Run();
            are.WaitOne();
            return stringBuilder.ToString();
        }

        private Activity CreateCodeOnlyWorkflow()
        {
            Variable<Employee> e1 = new Variable<Employee>("Employee1", ctx => new Employee("John", "Doe", 55000.0));
            Variable<Employee> e2 = new Variable<Employee>("Employee2", ctx => new Employee("Frank", "Kimono", 89000.0));
            Variable<SalaryStats> stats = new Variable<SalaryStats>("SalaryStats", ctx => new SalaryStats());
            Variable<Double> v1 = new Variable<double>();

            // The most efficient way of defining expressions in code is via LambdaValue and LambdaReference activities.
            // LambdaValue represents an expression that evaluates to an r-value and cannot be assigned to.
            // LambdaReference represents an expression that evaluates to an l-value and can be the target of an assignment.
            Sequence workflow = new Sequence()
            {
                Variables =
                {
                    e1, e2, stats, v1,
                },

                Activities =
                {
                    new WriteLine()
                    {
                        Text = new LambdaValue<string>(ctx => e1.Get(ctx).FirstName + " " + e1.Get(ctx).LastName + " earns " + e1.Get(ctx).Salary.ToString("$0.00")),
                    },
                    new WriteLine()
                    {
                        Text = new LambdaValue<string>(ctx => e2.Get(ctx).FirstName + " " + e2.Get(ctx).LastName + " earns " + e2.Get(ctx).Salary.ToString("$0.00")),
                    },
                    new Assign<double>()
                    {
                        To = new LambdaReference<double>(ctx => stats.Get(ctx).MinSalary),
                        Value = new LambdaValue<double>(ctx => Math.Min(e1.Get(ctx).Salary, e2.Get(ctx).Salary))
                    },
                    new Assign<double>()
                    {
                        To = new LambdaReference<double>(ctx => stats.Get(ctx).MaxSalary),
                        Value = new LambdaValue<double>(ctx => Math.Max(e1.Get(ctx).Salary, e2.Get(ctx).Salary))
                    },
                    new Assign<double>()
                    {
                        To = new LambdaReference<double>(ctx => stats.Get(ctx).AvgSalary),
                        Value = new LambdaValue<double>(ctx => (e1.Get(ctx).Salary + e2.Get(ctx).Salary) / 2.0)
                    },
                    new WriteLine()
                    {
                        Text = new LambdaValue<string>(ctx => String.Format(
                            "Salary statistics: minimum salary is {0:$0.00}, maximum salary is {1:$0.00}, average salary is {2:$0.00}",
                            stats.Get(ctx).MinSalary, stats.Get(ctx).MaxSalary, stats.Get(ctx).AvgSalary))
                    }
                },
            };

            return workflow;
        }

        private Activity CreateXamlSerializableCodeWorkflow()
        {
            Variable<Employee> e1 = new Variable<Employee> { Name = "Employee1", Default = ExpressionServices.Convert<Employee>(ctx => new Employee("John", "Doe", 55000.0)) };
            Variable<Employee> e2 = new Variable<Employee> { Name = "Employee2", Default = ExpressionServices.Convert<Employee>(ctx => new Employee("Frank", "Kimono", 89000.0)) };
            Variable<SalaryStats> stats = new Variable<SalaryStats> { Name = "SalaryStats", Default = ExpressionServices.Convert<SalaryStats>(ctx => new SalaryStats()) };
            Variable<Double> v1 = new Variable<double>();

            // Lambda expressions do not serialize to XAML.  ExpressionServices utility class can be used to 
            // convert them to operator activities, which do serialize to XAML.
            // ExpressionServices.Convert applies to r-values, which cannot be assigned to.
            // ExpressionServices.ConvertReference applies to l-values, which can be the target of an assignment.
            // Note that conversion is supported for a limited set of lambda expressions only.
            Sequence workflow = new Sequence()
            {
                Variables =
                {
                    e1, e2, stats, v1,
                },

                Activities =
                {
                    new WriteLine()
                    {
                        Text = ExpressionServices.Convert<string>(ctx => e1.Get(ctx).FirstName + " " + e1.Get(ctx).LastName + " earns " + e1.Get(ctx).Salary.ToString("$0.00")),
                    },
                    new WriteLine()
                    {
                        Text = ExpressionServices.Convert<string>(ctx => e2.Get(ctx).FirstName + " " + e2.Get(ctx).LastName + " earns " + e2.Get(ctx).Salary.ToString("$0.00")),
                    },
                    new Assign<double>()
                    {
                        To = ExpressionServices.ConvertReference<double>(ctx => stats.Get(ctx).MinSalary),
                        Value = ExpressionServices.Convert<double>(ctx => Math.Min(e1.Get(ctx).Salary, e2.Get(ctx).Salary))
                    },
                    new Assign<double>()
                    {
                        To = ExpressionServices.ConvertReference<double>(ctx => stats.Get(ctx).MaxSalary),
                        Value = ExpressionServices.Convert<double>(ctx => Math.Max(e1.Get(ctx).Salary, e2.Get(ctx).Salary))
                    },
                    new Assign<double>()
                    {
                        To = ExpressionServices.ConvertReference<double>(ctx => stats.Get(ctx).AvgSalary),
                        Value = ExpressionServices.Convert<double>(ctx => (e1.Get(ctx).Salary + e2.Get(ctx).Salary) / 2.0)
                    },
                    new WriteLine()
                    {
                        Text = ExpressionServices.Convert<string>(ctx => String.Format(
                            "Salary statistics: minimum salary is {0:$0.00}, maximum salary is {1:$0.00}, average salary is {2:$0.00}",
                            stats.Get(ctx).MinSalary, stats.Get(ctx).MaxSalary, stats.Get(ctx).AvgSalary))
                    }
                },
            };

            return workflow;
        }
    }

    public class Employee
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public double Salary { get; set; }

        public Employee()
        {
        }

        public Employee(string firstName, string lastName, double salary)
        {
            this.FirstName = firstName;
            this.LastName = lastName;
            this.Salary = salary;
        }
    }

    public struct SalaryStats
    {
        public double MinSalary { get; set; }
        public double MaxSalary { get; set; }
        public double AvgSalary { get; set; }
    }
}
