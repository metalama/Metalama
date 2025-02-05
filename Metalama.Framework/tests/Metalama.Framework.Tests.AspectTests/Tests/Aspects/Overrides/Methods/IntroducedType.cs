using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.IntroducedType;

internal class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var typeResult = builder.IntroduceClass( "TestType" );
        var methodResult = builder.With( typeResult.Declaration ).IntroduceMethod( nameof(IntroducedMethod) );

        builder.With( methodResult.Declaration ).Override( nameof(OverrideTemplate) );
    }

    [Template]
    public void IntroducedMethod()
    {
        Console.WriteLine( "Introduced Method" );
    }

    [Template]
    public dynamic? OverrideTemplate()
    {
        Console.WriteLine( "Override" );

        return meta.Proceed();
    }
}

// <target>
[Aspect]
internal class Target { }