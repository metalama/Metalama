using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33018;

public abstract class IntroductionAttribute : TypeAspect
{
    [Introduce( IsVirtual = false )]
    public virtual void M()
    {
        Console.WriteLine( "Base template (wrong)." );
    }
}

public class InheritedIntroductionAttribute : IntroductionAttribute
{
    public override void M()
    {
        Console.WriteLine( "Override template (expected)." );
    }
}

// <target>
[InheritedIntroduction]
internal class TargetClass { }