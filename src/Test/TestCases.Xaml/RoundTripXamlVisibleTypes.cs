// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.Generic;
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
                    new object[] { typeof(And<bool, bool, bool>) },
                    new object[] { typeof(ArrayItemReference<int>) },
                    new object[] { typeof(ArrayItemValue<int>) },
                    new object[] { typeof(As<object, object>) },
                    new object[] { typeof(Cast<object, object>) },
                    new object[] { typeof(Divide<int, int, int>) },
                    new object[] { typeof(Equal<string, string, bool>) },
                    new object[] { typeof(FieldReference<int, int>) },
                    new object[] { typeof(FieldValue<int, int>) },
                    new object[] { typeof(GreaterThan<int, int, bool>) },
                    new object[] { typeof(GreaterThanOrEqual<int, int, bool>) },
                    new object[] { typeof(IndexerReference<string, int>) },
                    //new object[] { typeof(InvokeFunc<>) },
                    //new object[] { typeof(InvokeFunc<,>) },
                    //new object[] { typeof(InvokeFunc<,,>) },
                    //new object[] { typeof(InvokeFunc<,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,,,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,,,,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,,,,,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,,,,,,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,,,,,,,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,,,,,,,,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,,,,,,,,,,,>) },
                    //new object[] { typeof(InvokeFunc<,,,,,,,,,,,,,,,>) },
                    //new object[] { typeof(InvokeMethod<>) },
                    //new object[] { typeof(LessThan<,,>) },
                    //new object[] { typeof(LessThanOrEqual<,,>) },
                    //new object[] { typeof(Literal<>) },
                    //new object[] { typeof(MultidimensionalArrayItemReference<>) },
                    //new object[] { typeof(Multiply<,,>) },
                    //new object[] { typeof(New<>) },
                    //new object[] { typeof(NewArray<>) },
                    //new object[] { typeof(Not<,>) },
                    //new object[] { typeof(NotEqual<,,>) },
                    //new object[] { typeof(Or<,,>) },
                    //new object[] { typeof(OrElse) },
                    //new object[] { typeof(PropertyReference<,>) },
                    //new object[] { typeof(PropertyValue<,>) },
                    //new object[] { typeof(Subtract<,,>) },
                    //new object[] { typeof(ValueTypeFieldReference<,>) },
                    //new object[] { typeof(ValueTypeIndexerReference<,>) },
                    //new object[] { typeof(ValueTypePropertyReference<,>) },
                    //new object[] { typeof(VariableReference<>) },
                    //new object[] { typeof(VariableValue<>) },
                    //new object[] { typeof(AddToCollection<>) },
                    //new object[] { typeof(Assign) },
                    //new object[] { typeof(Assign<>) },
                    //new object[] { typeof(Catch) },
                    //new object[] { typeof(Catch<>) },
                    //new object[] { typeof(ClearCollection<>) },
                    //new object[] { typeof(Delay) },
                    //new object[] { typeof(DoWhile) },
                    //new object[] { typeof(DurableTimerExtension) },
                    //new object[] { typeof(ExistsInCollection<>) },
                    //new object[] { typeof(Flowchart) },
                    //new object[] { typeof(FlowDecision) },
                    //new object[] { typeof(FlowNode) },
                    //new object[] { typeof(FlowStep) },
                    //new object[] { typeof(FlowSwitch<>) },
                    //new object[] { typeof(ForEach<>) },
                    //new object[] { typeof(If) },
                    //new object[] { typeof(InvokeMethod) },
                    //new object[] { typeof(Parallel) },
                    //new object[] { typeof(ParallelForEach<>) },
                    //new object[] { typeof(Persist) },
                    //new object[] { typeof(Pick) },
                    //new object[] { typeof(PickBranch) },
                    //new object[] { typeof(RemoveFromCollection<>) },
                    //new object[] { typeof(Rethrow) },
                    //new object[] { typeof(Sequence) },
                    //new object[] { typeof(Switch<>) },
                    //new object[] { typeof(TerminateWorkflow) },
                    //new object[] { typeof(Throw) },
                    //new object[] { typeof(TimerExtension) },
                    //new object[] { typeof(TryCatch) },
                    //new object[] { typeof(While) },
                    //new object[] { typeof(WorkflowTerminatedException) },
                    //new object[] { typeof(WriteLine) },
                    //new object[] { typeof(Assign) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(SysActTypes))]
        public void RoundTripObject(Type type)
        {
            Object[] instances = new Object[3];
            Object obj = null;
            DateTime now = DateTime.Now;
            int seed = 10000 * now.Year + 100 * now.Month + now.Day;
            Random rndGen = new Random(seed);

            obj = InstanceCreator.CreateInstanceOf(type, rndGen);
            if (obj != null && !(obj is MarkupExtension))
            {
                Object returnedObj = null;
                returnedObj = XamlTestDriver.RoundTripAndCompareObjects(obj, "CacheId", "Implementation", "ImplementationVersion");
                Assert.NotNull(returnedObj);
            }
        }

        [Theory]
        [InlineData(typeof(AndAlso))]
        public void DynamicImplCantSerialize(Type type)
        {
            try
            {
                RoundTripObject(type);
            }
            catch (System.Xaml.XamlObjectReaderException exc)
            {
                Assert.NotNull(exc.InnerException);
                Assert.Equal(typeof(System.NotSupportedException), exc.InnerException.GetType());
            }
        }
    }
}
