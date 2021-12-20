using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using System.Activities;
using Xunit;

namespace TestCases.Workflows
{
    public class DesignerHelperTests
    {
        [Fact]
        public void VisualBasicDesigner_CreatesValueProperly()
        {
            var sequence = Load(TestXamls.SalaryCalculation);

            var result = VisualBasicDesignerHelper.CreatePrecompiledValue(typeof(string), "MyVar", sequence, out _, out _);

            ((VisualBasicValue<string>)result).ExpressionText.ShouldBe("MyVar");
        }

        [Fact]
        public void VisualBasicDesigner_CreatesReferenceProperly()
        {
            var sequence = Load(TestXamls.SalaryCalculation);

            var result = VisualBasicDesignerHelper.CreatePrecompiledReference(typeof(string), "MyVar", sequence, out _, out _);

            ((VisualBasicReference<string>)result).ExpressionText.ShouldBe("MyVar");
        }

        [Fact]
        public void CSharpDesigner_CreatesValueProperly()
        {
            var sequence = Load(TestXamls.SalaryCalculation);

            var result = CSharpDesignerHelper.CreatePrecompiledValue(typeof(string), "MyVar", sequence, out _, out _);

            ((CSharpValue<string>)result).ExpressionText.ShouldBe("MyVar");
        }

        [Fact]
        public void CSharpDesigner_CreatesReferenceProperly()
        {
            var sequence = Load(TestXamls.SalaryCalculation);

            var result = CSharpDesignerHelper.CreatePrecompiledReference(typeof(string), "MyVar", sequence, out _, out _);

            ((CSharpReference<string>)result).ExpressionText.ShouldBe("MyVar");
        }

        private Activity Load(TestXamls xaml)
        {
            var root = TestHelper.GetActivityFromXamlResource(xaml);
            WorkflowInspectionServices.CacheMetadata(root);
            return root.ImplementationChildren[0];
        }
    }
}
