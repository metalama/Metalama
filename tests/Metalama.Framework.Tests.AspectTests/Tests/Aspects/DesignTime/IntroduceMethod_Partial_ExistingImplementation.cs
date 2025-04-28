#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceMethod_Partial_ExistingImplementation;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(PartialMethod) );
    }

    [Template(IsPartial = true)]
    private extern void PartialMethod();
}

// <target>
[Introduction]
internal partial class TargetClass 
{
#if TESTRUNNER
    partial void PartialMethod()
    {
        Console.WriteLine("Implementation");
    }
#endif
}