using System;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;
using TestCases.Xaml.Common.InstanceCreator;
using TestObjects.XamlTestDriver;
using Xunit;

namespace TestCases.Xaml
{
    public class RoundTripXamlVisibleTypes
    {
        public static IEnumerable<object[]> SysActTypes
        {
            get
            {
                return new object[][] {
                    new object[] { typeof(Add<int, int, int>) },
                    new object[] { typeof(And<,,>) },
                    new object[] { typeof(AndAlso) },
                    new object[] { typeof(ArrayItemReference<>) },
                    new object[] { typeof(ArrayItemValue<>) },
                    new object[] { typeof(As<,>) },
                    new object[] { typeof(Cast<,>) },
                    new object[] { typeof(Divide<,,>) },
                    new object[] { typeof(Equal<,,>) },
                    new object[] { typeof(ExpressionServices) },
                    new object[] { typeof(FieldReference<,>) },
                    new object[] { typeof(FieldValue<,>) },
                    new object[] { typeof(GreaterThan<,,>) },
                    new object[] { typeof(GreaterThanOrEqual<,,>) },
                    new object[] { typeof(IndexerReference<,>) },
                    new object[] { typeof(InvokeFunc<>) },
                    new object[] { typeof(InvokeFunc<,>) },
                    new object[] { typeof(InvokeFunc<,,>) },
                    new object[] { typeof(InvokeFunc<,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,,,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,,,,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,,,,,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,,,,,,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,,,,,,,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,,,,,,,,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,,,,,,,,,,,>) },
                    new object[] { typeof(InvokeFunc<,,,,,,,,,,,,,,,>) },
                    new object[] { typeof(InvokeMethod<>) },
                    new object[] { typeof(LessThan<,,>) },
                    new object[] { typeof(LessThanOrEqual<,,>) },
                    new object[] { typeof(Literal<>) },
                    new object[] { typeof(MultidimensionalArrayItemReference<>) },
                    new object[] { typeof(Multiply<,,>) },
                    new object[] { typeof(New<>) },
                    new object[] { typeof(NewArray<>) },
                    new object[] { typeof(Not<,>) },
                    new object[] { typeof(NotEqual<,,>) },
                    new object[] { typeof(Or<,,>) },
                    new object[] { typeof(OrElse) },
                    new object[] { typeof(PropertyReference<,>) },
                    new object[] { typeof(PropertyValue<,>) },
                    new object[] { typeof(Subtract<,,>) },
                    new object[] { typeof(ValueTypeFieldReference<,>) },
                    new object[] { typeof(ValueTypeIndexerReference<,>) },
                    new object[] { typeof(ValueTypePropertyReference<,>) },
                    new object[] { typeof(VariableReference<>) },
                    new object[] { typeof(VariableValue<>) },
                    new object[] { typeof(AddToCollection<>) },
                    new object[] { typeof(Assign) },
                    new object[] { typeof(Assign<>) },
                    new object[] { typeof(Catch) },
                    new object[] { typeof(Catch<>) },
                    new object[] { typeof(ClearCollection<>) },
                    new object[] { typeof(Delay) },
                    new object[] { typeof(DoWhile) },
                    new object[] { typeof(DurableTimerExtension) },
                    new object[] { typeof(ExistsInCollection<>) },
                    new object[] { typeof(Flowchart) },
                    new object[] { typeof(FlowDecision) },
                    new object[] { typeof(FlowNode) },
                    new object[] { typeof(FlowStep) },
                    new object[] { typeof(FlowSwitch<>) },
                    new object[] { typeof(ForEach<>) },
                    new object[] { typeof(If) },
                    new object[] { typeof(InvokeMethod) },
                    new object[] { typeof(Parallel) },
                    new object[] { typeof(ParallelForEach<>) },
                    new object[] { typeof(Persist) },
                    new object[] { typeof(Pick) },
                    new object[] { typeof(PickBranch) },
                    new object[] { typeof(RemoveFromCollection<>) },
                    new object[] { typeof(Rethrow) },
                    new object[] { typeof(Sequence) },
                    new object[] { typeof(Switch<>) },
                    new object[] { typeof(TerminateWorkflow) },
                    new object[] { typeof(Throw) },
                    new object[] { typeof(TimerExtension) },
                    new object[] { typeof(TryCatch) },
                    new object[] { typeof(While) },
                    new object[] { typeof(WorkflowTerminatedException) },
                    new object[] { typeof(WriteLine) },
                    new object[] { typeof(Assign) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(SysActTypes))]
        public void RoundTripObject(Type type)
        {
            Object[] instances = new Object[3];
            Object obj = null;
            //CreatorSettings.SetPOCONonPublicSetters = false;
            try
            {
                DateTime now = DateTime.Now;
                int seed = 10000 * now.Year + 100 * now.Month + now.Day;
                //Trace.WriteLine(string.Format("Seed: {0}", seed));
                Random rndGen = new Random(seed);

                obj = InstanceCreator.CreateInstanceOf(type, rndGen);
            }
            catch (Exception e)
            {
                //Trace.WriteLine(e.Message);
            }
            if (obj != null && !(obj is MarkupExtension))
            {
                Object returnedObj = null;
                try
                {
                    returnedObj = XamlTestDriver.RoundTripAndCompareObjects(obj);
                }
                catch (Exception e)
                {
                    //Trace.WriteLine(e.Message);
                    throw;
                }
                if (returnedObj == null)
                {
                    throw new Exception("returned object null");
                }

            }
            try
            {
                instances[0] = InstanceCreator.CreateInstanceOf(type, new Random(429321));
                instances[1] = InstanceCreator.CreateInstanceOf(type, new Random(118));
                instances[2] = InstanceCreator.CreateInstanceOf(type, new Random(99999));
            }
            catch (Exception e)
            {
                //Trace.WriteLine(e.Message);
            }
            foreach (Object instance in instances)
            {
                if (instance != null && !(instance is MarkupExtension))
                {
                    Object returnedObj = null;
                    try
                    {
                        returnedObj = XamlTestDriver.RoundTripAndCompareObjects(instance);
                    }
                    catch (Exception e)
                    {
                        //Trace.WriteLine(e.Message);
                        throw;
                    }
                    if (returnedObj == null)
                    {
                        throw new Exception("returned object null");
                    }

                }

            }
        }
    }
}
