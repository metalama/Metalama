using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.DeclarativeVirtual_CrossAssembly;

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