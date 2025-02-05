using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.IntroducedType;

internal class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var typeResult = builder.IntroduceClass( "TestType" );
        var methodResult = builder.With( typeResult.Declaration ).IntroduceProperty( nameof(IntroducedProperty) );

        builder.With( methodResult.Declaration ).Override( nameof(OverrideTemplate) );
    }

    [Template]
    public int IntroducedProperty { get; set; }

    [Template]
    public dynamic? OverrideTemplate
    {
        get
        {
            Console.WriteLine( "Override" );

            return meta.Proceed();
        }

        set
        {
            Console.WriteLine( "Override" );
            meta.Proceed();
        }
    }
}

// <target>
[Aspect]
internal class Target { }