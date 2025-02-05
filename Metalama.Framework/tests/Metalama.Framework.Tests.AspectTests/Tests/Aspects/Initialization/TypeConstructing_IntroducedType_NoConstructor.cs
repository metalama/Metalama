using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.TypeConstructing_IntroducedType_NoConstructor;

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedType = builder.IntroduceClass( "IntroducedType" ).Declaration;
        builder.With( introducedType ).AddInitializer( nameof(Template), InitializerKind.BeforeTypeConstructor );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( $"{meta.Target.Type.Name}: {meta.AspectInstance.AspectClass.ShortName}" );
    }
}

// <target>
[Aspect]
public class TargetCode { }