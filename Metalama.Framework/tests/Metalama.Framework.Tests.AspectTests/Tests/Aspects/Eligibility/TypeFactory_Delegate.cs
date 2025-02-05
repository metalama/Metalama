using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Eligibility.TypeFactory_Delegate;

internal class TestAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) { }

    public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
    {
        builder.MustSatisfy(
            _ =>
            {
                TypeFactory.GetType( typeof(RunTimeClass) );

                return true;
            },
            _ => $"" );
    }
}

internal class RunTimeClass { }

// <target>
[TestAspect]
internal class TargetClass { }