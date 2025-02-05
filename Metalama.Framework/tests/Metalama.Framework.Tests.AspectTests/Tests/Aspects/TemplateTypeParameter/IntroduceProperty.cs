using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.IntroduceProperty;

#pragma warning disable CS0219

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceProperty( "Property", nameof(GetTemplate), nameof(SetTemplate), args: new { T = builder.Target, x = 42 } );
        builder.IntroduceProperty( "Property_GetOnly", nameof(GetTemplate), null, args: new { T = builder.Target, x = 42 } );
        builder.IntroduceProperty( "Property_SetOnly", null, nameof(SetTemplate), args: new { T = builder.Target, x = 42 } );
    }

    [Template]
    private T? GetTemplate<[CompileTime] T>( [CompileTime] int x ) where T : class
    {
        return default;
    }

    [Template]
    private void SetTemplate<[CompileTime] T>( [CompileTime] int x, T p ) where T : class { }
}

// <target>
[Aspect]
public class Target { }