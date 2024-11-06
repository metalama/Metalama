using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Override_Method_Derived;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.ImplementInterface( typeof(IDisposable), whenExists: OverrideStrategy.Override );
    }

    [InterfaceMember]
    public void Dispose()
    {
        meta.Proceed();
        Console.WriteLine( "TheAspect.Dispose()" );
    }
}

internal class C : IDisposable
{
    public virtual void Dispose()
    {
        Console.WriteLine( "C.Dispose()" );
    }
}

// <target>
[TheAspect]
internal class D : C { }