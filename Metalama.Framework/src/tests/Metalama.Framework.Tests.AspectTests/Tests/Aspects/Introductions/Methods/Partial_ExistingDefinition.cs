#if TEST_OPTIONS
// @Skipped(#35847)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Partial_ExistingDefinition;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(PartialMethod) );
    }

    [Template(IsPartial = true)]
    private void PartialMethod()
    {
        Console.WriteLine("Implementation");
    }
}

// <target>
[Introduction]
internal partial class TargetClass
{
#if TESTRUNNER
    partial void PartialMethod();
#endif
}