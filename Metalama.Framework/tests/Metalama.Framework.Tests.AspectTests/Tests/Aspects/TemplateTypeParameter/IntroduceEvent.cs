using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.IntroduceEvent;

#pragma warning disable CS0219

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceEvent( "Event", nameof(Template), nameof(Template), args: new { T = typeof(EventHandler), x = 42 } );
    }

    [Template]
    private void Template<[CompileTime] T>( [CompileTime] int x, T p ) where T : class { }
}

// <target>
[Aspect]
public class Target { }