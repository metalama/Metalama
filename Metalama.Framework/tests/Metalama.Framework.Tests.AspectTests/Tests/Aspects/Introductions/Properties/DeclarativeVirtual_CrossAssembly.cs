using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.DeclarativeVirtual_CrossAssembly;

[RunTimeOrCompileTime]
public class InheritedIntroductionAttribute : IntroductionAttribute
{
    public override int VirtualOverriddenIntroduction
    {
        get
        {
            Console.WriteLine( "Base template (expected)." );

            return 42;
        }
        set
        {
            Console.WriteLine( "Base template (expected)." );
        }
    }
}

// <target>
[InheritedIntroduction]
internal class TargetClass { }