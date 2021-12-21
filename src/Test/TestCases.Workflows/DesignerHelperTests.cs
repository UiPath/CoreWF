using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using System.Activities;
using System.Activities.Statements;
using Xunit;

namespace TestCases.Workflows;

public class DesignerHelperTests
{
    [Fact]
    public void VisualBasicDesigner_CreatesValueProperly()
    {
        var result = VisualBasicDesignerHelper.CreatePrecompiledValue(typeof(string), "MyVar", NewSequence(), out _, out _);

        ((VisualBasicValue<string>)result).ExpressionText.ShouldBe("MyVar");
    }

    [Fact]
    public void VisualBasicDesigner_CreatesReferenceProperly()
    {
        var result = VisualBasicDesignerHelper.CreatePrecompiledReference(typeof(string), "MyVar", NewSequence(), out _, out _);

        ((VisualBasicReference<string>)result).ExpressionText.ShouldBe("MyVar");
    }

    [Fact]
    public void CSharpDesigner_CreatesValueProperly()
    {
        var result = CSharpDesignerHelper.CreatePrecompiledValue(typeof(string), "MyVar", NewSequence(), out _, out _);

        ((CSharpValue<string>)result).ExpressionText.ShouldBe("MyVar");
    }

    [Fact]
    public void CSharpDesigner_CreatesReferenceProperly()
    {
        var result = CSharpDesignerHelper.CreatePrecompiledReference(typeof(string), "MyVar", NewSequence(), out _, out _);

        ((CSharpReference<string>)result).ExpressionText.ShouldBe("MyVar");
    }

    private Sequence NewSequence()
    {
        var root = new Sequence();
        WorkflowInspectionServices.CacheMetadata(root);
        return root;
    }
}
