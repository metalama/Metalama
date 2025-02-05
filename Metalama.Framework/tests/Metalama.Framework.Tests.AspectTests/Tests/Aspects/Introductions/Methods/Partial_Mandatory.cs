using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Partial_Mandatory;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(PublicMethod) );
        builder.IntroduceMethod( nameof(NonVoidMethod) );
    }

    [Template(IsPartial = true)]
    public extern void PublicMethod();

    [Template(IsPartial = true)]
    private extern int NonVoidMethod();
}

// <target>
[Introduction]
internal partial class TargetClass { }