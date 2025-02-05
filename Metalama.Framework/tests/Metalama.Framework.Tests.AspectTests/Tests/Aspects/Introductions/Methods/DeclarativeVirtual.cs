using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.DeclarativeVirtual;

public abstract class IntroductionAttribute : TypeAspect
{
    [Introduce( IsVirtual = false )]
    public virtual void VirtualOverriddenIntroduction()
    {
        Console.WriteLine( "Base template (wrong)." );
    }

    [Introduce( IsVirtual = false )]
    public virtual void VirtualIntroduction()
    {
        Console.WriteLine( "Base template (expected)." );
    }
}

public class InheritedIntroductionAttribute : IntroductionAttribute
{
    public override void VirtualOverriddenIntroduction()
    {
        Console.WriteLine( "Override template (expected)." );
    }
}

// <target>
[InheritedIntroduction]
internal class TargetClass { }