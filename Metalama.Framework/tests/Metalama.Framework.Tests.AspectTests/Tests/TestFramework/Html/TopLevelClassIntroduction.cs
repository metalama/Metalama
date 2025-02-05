

// This test verifies that there is no error when writing the HTML file for an introduced syntax tree,
// but it does not test the HTML itself.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.TestFramework.Html.TopLevelClassIntroduction;

[assembly: IntroduceTopLevelClass]
namespace Metalama.Framework.IntegrationTests.TestFramework.Html.TopLevelClassIntroduction
{
    public class IntroduceTopLevelClassAttribute : CompilationAspect
    {
        public override void BuildAspect( IAspectBuilder<ICompilation> builder )
        {
            var ns = builder.WithNamespace( "Some.Namespace" );
            ns.IntroduceClass( "SomeClass" );
        }
    }
}