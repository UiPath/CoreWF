using Shouldly;
using System;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.Statements.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace TestCases.Activities.Interfaces;

using Flowchart = System.Activities.Statements.Flowchart;

public sealed class FlowchartInterfacesTests
{
    [Fact]
    public void Given_FlowChartImplementation_Then_ExpectedInterfacesAreImplemented()
    {
        typeof(Flowchart).IsAssignableTo(typeof(IFlowchart)).ShouldBeTrue();
        typeof(IFlowchart).IsAssignableTo(typeof(IHasVariables)).ShouldBeTrue();
        AssertSignature(typeof(IFlowchart), "97ecf192e1348f76da5a721b592dddd1218bd3505aab78e4b6fe3b05a0279492");
    }

    [Fact]
    public void Given_FlowChartImplementation_Then_ExplicitNodesReturnsNodes()
    {
        var flowChart = new Flowchart();
        flowChart.Nodes.Add(new FlowStep());
        var explicitImpl = flowChart as IFlowchart;
        explicitImpl.Nodes.ShouldBe(flowChart.Nodes);
    }

    [Fact]
    public void Given_FlowChartImplementation_Then_ExplicitStartNodeReturnsStartNode()
    {
        var flowChart = new Flowchart
        {
            StartNode = new FlowStep()
        };

        var explicitImpl = flowChart as IFlowchart;
        explicitImpl.StartNode.ShouldBe(flowChart.StartNode);

        explicitImpl.StartNode = new FlowDecision();
        flowChart.StartNode.ShouldBe(explicitImpl.StartNode);
    }

    [Fact]
    public void Given_FlowChartImplementation_Then_HasFlowStepTypeAttribute()
    {
        var attr = typeof(Flowchart).GetCustomAttribute(typeof(FlowStepTypeAttribute)) as FlowStepTypeAttribute;
        attr.ShouldNotBeNull();
        attr.FlowStepType.ShouldBe(typeof(FlowStep));
        AssertSignature(typeof(FlowStepTypeAttribute), "5fe4c4c48d8bf4289c9d5b17bc2ab2b67770afff946b4bcb8773ea01c54d00b2");
    }

    [Fact]
    public void Given_FlowDecisionImplementation_Then_ExpectedInterfacesAreImplemented()
    {
        typeof(FlowDecision).IsAssignableTo(typeof(IFlowDecision)).ShouldBeTrue();
        typeof(IFlowDecision).IsAssignableTo(typeof(IFlowNode)).ShouldBeTrue();
        typeof(IFlowDecision).IsAssignableTo(typeof(IHasDisplayName)).ShouldBeTrue();
        typeof(IFlowDecision).IsAssignableTo(typeof(IHasCondition)).ShouldBeTrue();
        AssertSignature(typeof(IFlowDecision), "586b121796fc32cbb96cc1d22862230cbc68988f0a51447332da60f0a0c6c24c");
    }

    [Fact]
    public void Given_FlowDecisionImplementation_Then_ExplicitFalseReturnsFalse()
    {
        var flowDecision = new FlowDecision() { False = new FlowStep { Action = Literal<int>.FromValue(2) } };
        var explicitImpl = flowDecision as IFlowDecision;
        explicitImpl.False.ShouldBe(flowDecision.False);
        explicitImpl.False = new FlowStep { Action = Literal<int>.FromValue(3) };
        flowDecision.False.ShouldBe(explicitImpl.False);
    }

    [Fact]
    public void Given_FlowDecisionImplementation_Then_ExplicitTrueReturnsTrue()
    {
        var flowDecision = new FlowDecision() { True = new FlowStep { Action = Literal<int>.FromValue(2) } };
        var explicitImpl = flowDecision as IFlowDecision;
        explicitImpl.True.ShouldBe(flowDecision.True);
        explicitImpl.True = new FlowStep { Action = Literal<int>.FromValue(3) };
        flowDecision.True.ShouldBe(explicitImpl.True);
    }

    [Fact]
    public void Given_FlowNodeImplementation_Then_ExpectedInterfacesAreImplemented()
    {
        typeof(FlowNode).IsAssignableTo(typeof(IFlowNode)).ShouldBeTrue();
        AssertSignature(typeof(IFlowNode), "fcd5cd2288620dc8341a60adf0d5c2dedeba1c904c3e6564119cb7a0743daf37");
    }

    [Fact]
    public void Given_FlowStepImplementation_Then_ExpectedInterfacesAreImplemented()
    {
        typeof(FlowStep).IsAssignableTo(typeof(IFlowStep)).ShouldBeTrue();
        typeof(IFlowStep).IsAssignableTo(typeof(IFlowNode)).ShouldBeTrue();
        typeof(IFlowStep).IsAssignableTo(typeof(IHasAction)).ShouldBeTrue();
        AssertSignature(typeof(IFlowStep), "e23f616d8732acc56f5dd77d55a479d8bbce7213c878ea86dc03cce409eb33a5");
    }

    [Fact]
    public void Given_FlowStepImplementation_Then_ExplicitNextReturnsNext()
    {
        var flowStep = new FlowStep();
        flowStep.Next = new FlowStep { Action = Literal<bool>.FromValue(true) };
        var explicitImpl = flowStep as IFlowStep;
        explicitImpl.Next.ShouldBe(flowStep.Next);

        explicitImpl.Next = new FlowStep { Action = Literal<bool>.FromValue(false) };
        flowStep.Next.ShouldBe(explicitImpl.Next);
    }

    [Fact]
    public void Given_FlowStepImplementation_Then_GetConnectedNodesReturnsNextStep()
    {
        var flowStep = new FlowStep();
        flowStep.Next = new FlowStep { Action = Literal<bool>.FromValue(true) };
        AssertGetConnectedNodes(flowStep, [flowStep.Next]);
    }

    [Fact]
    public void Given_FlowSwitchImplementation_Then_ExpectedInterfacesAreImplemented()
    {
        typeof(FlowSwitch<int>).IsAssignableTo(typeof(IFlowSwitch<int>)).ShouldBeTrue();
        typeof(FlowSwitch<int>).IsAssignableTo(typeof(IFlowSwitch)).ShouldBeTrue();
        typeof(IFlowSwitch<int>).IsAssignableTo(typeof(IFlowSwitch)).ShouldBeTrue();
        typeof(IFlowSwitch<int>).IsAssignableTo(typeof(IHasExpression<int>)).ShouldBeTrue();
        typeof(IFlowSwitch).IsAssignableTo(typeof(IHasExpressionNonGeneric)).ShouldBeTrue();
        typeof(IFlowSwitch).IsAssignableTo(typeof(IFlowNode)).ShouldBeTrue();
        typeof(IFlowSwitch).IsAssignableTo(typeof(IHasDisplayName)).ShouldBeTrue();
        AssertSignature(typeof(IFlowSwitch<int>), "c354696d185d1a16845668debe80f632ad6d95516f81f8c2a04ab2c10024aa68");
    }

    [Fact]
    public void Given_FlowSwitchImplementation_Then_ExplicitCasesReturnsCases()
    {
        var flowSwitch = new FlowSwitch<int>();
        flowSwitch.Cases[0] = new FlowStep { Action = Literal<int>.FromValue(0) };
        flowSwitch.Cases[1] = new FlowStep { Action = Literal<int>.FromValue(1) };
        flowSwitch.Cases[2] = new FlowStep { Action = Literal<int>.FromValue(2) };

        var explicitImpl = flowSwitch as IFlowSwitch<int>;
        var cases = explicitImpl.Cases;
        cases.Count.ShouldBe(flowSwitch.Cases.Count);
        foreach (var pair in cases)
        {
            flowSwitch.Cases[pair.Key].ShouldBe(pair.Value);
        }

        explicitImpl.CaseNodes.ShouldBe(flowSwitch.Cases.Values);
    }

    [Fact]
    public void Given_FlowSwitchImplementation_Then_ExplicitDefaultReturnsDefault()
    {
        var flowSwitch = new FlowSwitch<int>();
        flowSwitch.Default = new FlowStep { Action = Literal<int>.FromValue(2) };
        var explicitImpl = flowSwitch as IFlowSwitch<int>;
        explicitImpl.Default.ShouldBe(flowSwitch.Default);

        explicitImpl.Default = new FlowStep { Action = Literal<int>.FromValue(3) };
        flowSwitch.Default.ShouldBe(explicitImpl.Default);
    }

    [Fact]
    public void Given_FlowSwitchImplementation_Then_ExpressionNonGenericReturnsExpression()
    {
        var flowSwitch = new FlowSwitch<int>();
        AssertExpressionNonGeneric(flowSwitch);
    }

    private static void AssertExpressionNonGeneric(FlowSwitch<int> flowSwitch)
    {
        flowSwitch.Expression = Literal<int>.FromValue(3);
        (flowSwitch as IHasExpressionNonGeneric).ExpressionNonGeneric.ShouldBe(flowSwitch.Expression);
    }

    private static void AssertGetConnectedNodes(FlowNode flowNode, IEnumerable<IFlowNode> expectedNodes)
    {
        (flowNode as IFlowNode).GetConnectedNodes().ShouldBe(expectedNodes);
    }

    private static void AssertSignature(Type type, string expectedSignature)
    {
        GetSignature(type).ShouldBe(expectedSignature, $"The type '{type.Name}' or one of the interfaces it implements does not have the expected signature. " +
            $"Please do not modify this type, as it must remain compatible with existing implementations. Add new types if necessary.");
    }

    private static string GetSignature(Type type)
    {
        IEnumerable<string> names =
            type.GetMethods().OfType<MemberInfo>()
            .Concat(type.GetProperties())
            .Select(m => m.Name)
            .Concat(type.GetInterfaces().Select(t => t.FullName))
            .Concat(type.GetInterfaces().Select(GetSignature));
        var hashInput = string.Join(Environment.NewLine, names);
        using var hashAlg = SHA256.Create();
        var hash = hashAlg.ComputeHash(Encoding.UTF8.GetBytes(hashInput));

        var sb = new StringBuilder(hash.Length * 2);
        for (var i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("x2"));
        }

        return sb.ToString();
    }
}