using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Events.DeclarativeVirtual_CrossAssembly;

public abstract class IntroductionAttribute : TypeAspect
{
    [Introduce( IsVirtual = false )]
    public virtual event EventHandler VirtualOverriddenIntroduction
    {
        add
        {
            Console.WriteLine( "Base template (wrong)." );
        }
        remove
        {
            Console.WriteLine( "Base template (wrong)." );
        }
    }

    [Introduce( IsVirtual = false )]
    public virtual event EventHandler VirtualIntroduction
    {
        add
        {
            Console.WriteLine( "Base template (expected)." );
        }
        remove
        {
            Console.WriteLine( "Base template (expected)." );
        }
    }
}