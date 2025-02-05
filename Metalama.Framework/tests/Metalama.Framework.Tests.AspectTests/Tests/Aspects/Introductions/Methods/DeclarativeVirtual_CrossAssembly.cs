using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.DeclarativeVirtual_CrossAssembly;

[RunTimeOrCompileTime]
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