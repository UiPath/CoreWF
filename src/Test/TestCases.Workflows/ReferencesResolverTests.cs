using Microsoft.VisualBasic.Activities;
using System;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Shouldly;
using System.Activities.Expressions;
using System.Reflection;
using System.Runtime.Loader;

namespace TestCases.Workflows
{
    public class ReferencesResolverTests
    {
        private Func<AssemblyName, Assembly> _resolver = (name) =>
        {
            foreach (var assembly in AssemblyLoadContext.All.SelectMany(ctx => ctx.Assemblies))
            {
                var other = assembly.GetName();

                if(name.Name == other.Name && name.Version == other.Version && name.GetPublicKeyToken().SequenceEqual(other.GetPublicKeyToken()))
                {
                    return assembly;
                }
            }

            return null;
        };

        [Fact]
        public void Validation_WithoutMetadataReferenceResolver_ShouldFail()
        {
            ValidationResults validationResults = Validate(false);
            validationResults.Errors.Any(e => e.Message.Contains("Reference required to assembly 'System.Memory")).ShouldBeTrue();
        }

        [Fact]
        public void Validation_WithoutMetadataReferenceResolver_ShouldSucceed()
        {
            ValidationResults validationResults = Validate(true);
            validationResults.Errors.ShouldBeEmpty();
        }

        private ValidationResults Validate(bool useMetadataReferenceResolver)
        {
            string expression = @"System.Text.Json.JsonDocument.Parse("""").ToString()";

            VisualBasicValue<string> vbv = new(expression);
            WriteLine writeLine = new();
            writeLine.Text = new InArgument<string>(vbv);
            Sequence workflow = new();
            workflow.Activities.Add(writeLine);

            TextExpression.SetReferencesForImplementation(workflow, new[] { new AssemblyReference("System.Text.Json") });
            TextExpression.SetNamespacesForImplementation(workflow, new[] { "System.Text.Json" });

            var settings = new ValidationSettings();
            settings.SkipExpressionCompilation = false;
            settings.SkipImplementationChildren = false;
            settings.SkipValidatingRootConfiguration = false;
            settings.ForceExpressionCache = false;

            if (useMetadataReferenceResolver)
            {
                settings.MissingAssemblyResolver = _resolver;
            }

            ValidationResults validationResults = ActivityValidationServices.Validate(workflow, settings);
            return validationResults;
        }
    }
}
