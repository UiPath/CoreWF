using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Xaml;
using TestCases.Workflows.TestUtils;
using Xunit;

namespace TestCases.Workflows.WF4Samples
{
    using StringDictionary = Dictionary<string, object>;

    public abstract class ExpressionsBase
    {
        protected abstract bool CompileExpressions { get; }
        protected Activity GetActivityFromXamlResource(TestXamls xamlName) => TestHelper.GetActivityFromXamlResource(xamlName, CompileExpressions);
        protected Activity Compile(TestXamls xamlName)
        {
            var activity = GetActivityFromXamlResource(xamlName);
            Compiler.Run(activity);
            return activity;
        }
        protected const string CorrectOutput = @"John Doe earns $55000.00
Frank Kimono earns $89000.00
Salary statistics: minimum salary is $55000.00, maximum salary is $89000.00, average salary is $72000.00
";

        [Fact]
        public void SalaryCalculation()
        {
            var activity = GetActivityFromXamlResource(TestXamls.SalaryCalculation);
            TestHelper.InvokeWorkflow(activity).ShouldBe(CorrectOutput);
        }

        protected const string ForEachCorrectOutput = @"Iterate Array
...bill
...steve
...ray
Iterate ArrayList
...Bob
...John
";

        [Fact]
        public void NonGenericForEach()
        {
            var activity = GetActivityFromXamlResource(TestXamls.NonGenericForEach);
            TestHelper.InvokeWorkflow(activity).ShouldBe(ForEachCorrectOutput);
        }
    }

    public class JustInTimeExpressions : ExpressionsBase
    {
        protected override bool CompileExpressions => false;
        [Fact]
        public void CompileSalaryCalculation()
        {
            var activity = Compile(TestXamls.SalaryCalculation);
            TestHelper.InvokeWorkflow(activity).ShouldBe(CorrectOutput);
        }
        [Fact]
        public void CompileNonGenericForEach()
        {
            var activity = Compile(TestXamls.NonGenericForEach);
            TestHelper.InvokeWorkflow(activity).ShouldBe(ForEachCorrectOutput);
        }
    }

    /// <summary>
    /// These tests are taken from the 
    /// <a href="https://social.technet.microsoft.com/wiki/contents/articles/12326.windows-workflow-wf-4-x-samples.aspx">WF 4.0 Samples</a>
    /// under Basic/Expressions
    /// </summary>
    public class AheadOfTimeExpressions : ExpressionsBase
    {
        protected override bool CompileExpressions => true;
        static readonly string CSharpCalculationResult = "Result == XX^2" + Environment.NewLine;
        static readonly StringDictionary CSharpCalculationInputs = new() { ["XX"] = 16, ["YY"] = 16 };
        [Fact]
        public void VBSameTextDifferentTypes()
        {
            var text = new VisualBasicValue<string>("var");
            var values = new VisualBasicValue<IEnumerable<char>>("var");
            var root = new DynamicActivity { Implementation = () => new Sequence
            {
                Variables = { new Variable<string>("var") },
                Activities = { new ForEach<char> { Values = new InArgument<IEnumerable<char>>(values) }, new WriteLine { Text = new InArgument<string>(text) } }
            }};
            ActivityXamlServices.Compile(root, new());
            ((LambdaExpression)text.GetExpressionTree()).ReturnType.ShouldBe(typeof(string));
            ((LambdaExpression)values.GetExpressionTree()).ReturnType.ShouldBe(typeof(IEnumerable<char>));
        }
        [Fact]
        public void CSSameTextDifferentTypes()
        {
            var text = new CSharpValue<string>("var");
            var values = new CSharpValue<IEnumerable<char>>("var");
            var root = new DynamicActivity
            {
                Implementation = () => new Sequence
                {
                    Variables = { new Variable<string>("var") },
                    Activities = { new ForEach<char> { Values = new InArgument<IEnumerable<char>>(values) }, new WriteLine { Text = new InArgument<string>(text) } }
                }
            };
            ActivityXamlServices.Compile(root, new());
            ((LambdaExpression)text.GetExpressionTree()).ReturnType.ShouldBe(typeof(string));
            ((LambdaExpression)values.GetExpressionTree()).ReturnType.ShouldBe(typeof(IEnumerable<char>));
        }
        [Fact]
        public void CompileCSharpCalculation()
        {
            var activity = Compile(TestXamls.CSharpCalculation);
            TestHelper.InvokeWorkflow(activity, CSharpCalculationInputs).ShouldBe(CSharpCalculationResult);
        }
        [Fact]
        public void CSharpCalculation()
        {
            var activity = GetActivityFromXamlResource(TestXamls.CSharpCalculation);
            TestHelper.InvokeWorkflow(activity, CSharpCalculationInputs).ShouldBe(CSharpCalculationResult);

            CSharpDesignerHelper.CreatePrecompiledReference(typeof(int), "Result", Array.Empty<string>(), Array.Empty<string>(), activity.ImplementationEnvironment,
                out var type, out var expressionException, out var _);
            type.ShouldBe(typeof(int));
            expressionException.ShouldBeNull();
        }
        [Fact]
        public void CSharpCalculationGenerated()
        {
            var activity = TestHelper.GetActivityFromXamlResource(TestXamls.CSharpCalculation);
            CompiledExpressionInvoker.SetCompiledExpressionRootForImplementation(activity, new Calculation_CompiledExpressionRoot(activity));
            var inputs = new StringDictionary { ["XX"] = 16, ["YY"] = 16 };
            TestHelper.InvokeWorkflow(activity, inputs).ShouldBe("Result == XX^2" + Environment.NewLine);
        }
        [Fact]
        public async Task WorkflowWithReadonlyValueTypeVar()
        {
            var activity = TestHelper.GetActivityFromXamlResource(TestXamls.WorkflowWithReadonlyValueTypeVar);
            var compiledExpressionRoot = await GenerateAndLoadCompiledExpressionRoot(activity);
            CompiledExpressionInvoker.SetCompiledExpressionRootForImplementation(activity, compiledExpressionRoot);

            TestHelper.InvokeWorkflow(activity).ShouldBe(string.Empty);
        }
        private static async Task<ICompiledExpressionRoot> GenerateAndLoadCompiledExpressionRoot(Activity activity)
        {
            using var expressionsStringWriter = new StringWriter();
            var activityName = $"{TestXamls.WorkflowWithReadonlyValueTypeVar}_CompiledExpressionRoot";
            TextExpressionCompilerSettings settings = new()
            {
                Activity = activity,
                Language = "C#",
                ActivityName = activityName,
                RootNamespace = null,
                GenerateAsPartialClass = false,
                AlwaysGenerateSource = true,
                ForImplementation = true
            };
            new TextExpressionCompiler(settings).GenerateSource(expressionsStringWriter);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(References.GetReference).ToArray();
            var project = new Project(assemblies);
            var compiledExpressionsClassType = await project.Compile(expressionsStringWriter.ToString(), activityName);
            return (ICompiledExpressionRoot)Activator.CreateInstance(compiledExpressionsClassType, activity);
        }
        [Fact]
        public void Code()
        {
            var activity = CreateCodeOnlyWorkflow();
            TestHelper.InvokeWorkflow(activity).ShouldBe(CorrectOutput);
        }
        [Fact]
        public void InArgumentFromLocationName()
        {
            Sequence sequence = new() { Variables = { new Variable<string>("var", "value") }, 
                Activities = { new WriteLine { Text = InArgument<string>.FromLocationName("var") } } };
            TestHelper.InvokeWorkflow(sequence).ShouldBe("value"+Environment.NewLine);
        }
        string _value;
        [Fact]
        public void FuncReferenceNoLocation()
        {
            Sequence sequence = new()
            {
                Activities = 
                {
                    new Assign { To = (OutArgument<string>) new FuncReference<int, string>(null, _ => _value, (_, value) => { _value = value; return 0; }),
                        Value = (InArgument<string>)"newValue" },
                    new WriteLine { Text = new FuncValue<string>(_=>_value) }
                }
            };
            TestHelper.InvokeWorkflow(sequence).ShouldBe("newValue" + Environment.NewLine);
        }
        [Fact]
        public void FuncCode()
        {
            var activity = FuncCodeOnlyWorkflow();
            TestHelper.InvokeWorkflow(activity).ShouldBe(CorrectOutput);
            static Activity FuncCodeOnlyWorkflow() => CreateSequence(
                variables: new Variable[]{
                    CreateVariable(name: "Employee1", @default: ctx => new Employee("John", "Doe", 55000.0)),
                    CreateVariable(name: "Employee2", @default: ctx => new Employee("Frank", "Kimono", 89000.0)),
                    CreateVariable(name: "SalaryStats", @default: ctx => new SalaryStats()),
                    CreateVariable<double>(name: "average")},
                activities: new Activity[]{
                    CreateWriteLine(text: ctx => ctx.GetValue<Employee>("Employee1").FirstName + " " + ctx.GetValue<Employee>("Employee1").LastName + " earns " + ctx.GetValue<Employee>("Employee1").Salary.ToString("$0.00")),
                    CreateWriteLine(text: ctx => ctx.GetValue<Employee>("Employee2").FirstName + " " + ctx.GetValue<Employee>("Employee2").LastName + " earns " + ctx.GetValue<Employee>("Employee2").Salary.ToString("$0.00")),
                    CreateAssign(to: (OutArgument<double>)new FuncReference<SalaryStats, double>("SalaryStats", s => s.MinSalary, (s, value) =>
                    {
                        s.MinSalary = value;
                        return s;
                    }), value: (InArgument<double>)(Func<ActivityContext, double>)(ctx => Math.Min(ctx.GetValue<Employee>("Employee1").Salary, ctx.GetValue<Employee>("Employee2").Salary))),
                    CreateAssign(to: (OutArgument<double>)new FuncReference<SalaryStats, double>("SalaryStats", s => s.MaxSalary, (s, value) =>
                    {
                        s.MaxSalary = value;
                        return s;
                    }), value:  (InArgument<double>)(Func<ActivityContext, double>)(ctx => Math.Max(ctx.GetValue<Employee>("Employee1").Salary, ctx.GetValue<Employee>("Employee2").Salary))),
                    CreateAssign(to: (OutArgument<double>)new FuncReference<SalaryStats, double>("SalaryStats", s => s.AvgSalary, (s, value) =>
                    {
                        s.AvgSalary = value;
                        return s;
                    }), value:  (InArgument<double>)(Func<ActivityContext, double>)(ctx => (ctx.GetValue<Employee>("Employee1").Salary + ctx.GetValue<Employee>("Employee2").Salary) / 2.0)),
                    CreateAssign(to: (OutArgument<double>)new Reference<double>("average"), value:  (InArgument<double>)(Func<ActivityContext, double>)
                        (ctx => ctx.GetValue<SalaryStats>("SalaryStats").AvgSalary)),
                    CreateWriteLine(text: ctx => string.Format("Salary statistics: minimum salary is {0:$0.00}, maximum salary is {1:$0.00}, average salary is {2:$0.00}",
                        ctx.GetValue<SalaryStats>("SalaryStats").MinSalary, ctx.GetValue<SalaryStats>("SalaryStats").MaxSalary, ctx.GetValue<SalaryStats>("SalaryStats").AvgSalary))});
            static Variable<T> CreateVariable<T>(string name = null, Func<ActivityContext, T> @default = null)
            {
                var variable = new Variable<T>();
                if (name != null)
                {
                    variable.Name = name;
                }
                if (@default != null)
                {
                    variable.Default = @default;
                }
                return variable;
            }
            static Sequence CreateSequence(string displayName = null, Variable[] variables = null, Activity[] activities = null)
            {
                var sequence = new Sequence();
                if (displayName != null)
                {
                    sequence.DisplayName = displayName;
                }
                if (variables != null)
                {
                    sequence.Variables.AddRange(variables);
                }
                if (activities != null)
                {
                    sequence.Activities.AddRange(activities);
                }
                return sequence;
            }
            static WriteLine CreateWriteLine(Func<ActivityContext, string> text = null, string displayName = null)
            {
                var writeLine = new WriteLine();
                if (text != null)
                {
                    writeLine.Text = text;
                }
                if (displayName != null)
                {
                    writeLine.DisplayName = displayName;
                }
                return writeLine;
            }
        }
        //static Assign<T> CreateAssign<T>(Activity<Location<T>> to = null, Func<ActivityContext, T> value = null)
        //{
        //    var assign = new Assign<T>();
        //    if (to != null)
        //    {
        //        assign.To = to;
        //    }
        //    if (value != null)
        //    {
        //        assign.Value = value;
        //    }
        //    return assign;
        //}
        static Assign CreateAssign(OutArgument to = null, InArgument value = null)
        {
            var assign = new Assign();
            if (to != null)
            {
                assign.To = to;
            }
            if (value != null)
            {
                assign.Value = value;
            }
            return assign;
        }
        [Fact]
        public void CodeToXaml()
        {
            var activity = CreateXamlSerializableCodeWorkflow();
            string workflowXamlString = XamlServices.Save(activity);
            activity = (Activity)XamlServices.Load(new StringReader(workflowXamlString));
            TestHelper.InvokeWorkflow(activity).ShouldBe(CorrectOutput);
        }

        WriteLine CreateWriteLine(string text, string displayName, TextWriter textWriter) =>
            new()
            {
                Text = text,
                DisplayName = displayName,
                TextWriter = textWriter
            };

        WriteLine CreateWriteLine(Func<ActivityContext, string> text, string displayName, TextWriter textWriter) =>
            new()
            {
                Text = new FuncValue<string>(text),
                DisplayName = displayName,
                TextWriter = textWriter
            };

        void CreateWriteLine() => CreateWriteLine(text: "text", displayName: "display", textWriter: new StreamWriter(""));

        void NewWriteLine() => new WriteLine
        {
            Text = "text",
            DisplayName = "display",
            TextWriter = new StreamWriter("")
        };

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
                        To = new LambdaReference<double>(ctx => ctx.GetValue<SalaryStats>("SalaryStats").MinSalary),
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
    /// <summary>
    /// Simple X*Y=Z. CodeActivity with typed output through generic.
    /// </summary>
    public class Multiply : CodeActivity<int>
    {
        protected override int Execute(CodeActivityContext context)
        {
            var r = X.Get(context) * Y.Get(context);
            Z.Set(context, r);
            System.Diagnostics.Debug.WriteLine("Multiply done");
            return r;
        }

        [RequiredArgument]//https://msdn.microsoft.com/en-us/library/ee358733%28v=vs.110%29.aspx
        public InArgument<int> X { get; set; }

        [RequiredArgument]
        public InArgument<int> Y { get; set; }

        /// <summary>
        /// This is compiled however in production codes, OutArgument should not be defined.
        /// </summary>
        public OutArgument<int> Z { get; set; }

    }
    static class Helpers
    {
        public static void AddRange<T>(this Collection<T> collection, T[] itemsToAdd)
        {
            foreach (var item in itemsToAdd)
            {
                collection.Add(item);
            }
        }
    }
}