using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Partial;

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
internal partial class TargetClass { }