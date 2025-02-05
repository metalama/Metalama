using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Override_Method_Derived_FromAspect;

[Inheritable]
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.ImplementInterface( typeof(IDisposable), whenExists: OverrideStrategy.Override );
    }

    [InterfaceMember]
    public virtual void Dispose()
    {
        meta.Proceed();
        Console.WriteLine( "TheAspect.Dispose()" );
    }
}

// <target>
[TheAspect]
internal class C { }

// <target>
internal class D : C { }