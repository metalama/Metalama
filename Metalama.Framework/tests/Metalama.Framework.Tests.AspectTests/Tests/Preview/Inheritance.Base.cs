using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Preview.BasicTest.Inheritance;

[Inheritable]
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.ImplementInterface( typeof(IDisposable), whenExists: OverrideStrategy.Ignore );
        builder.IntroduceMethod( nameof(ProtectedDispose), whenExists: OverrideStrategy.Override );
    }

    [InterfaceMember]
    public virtual void Dispose()
    {
        meta.Proceed();
    }

    [Template(Name="Dispose")]
    protected virtual void ProtectedDispose( bool disposing )
    {
        meta.Proceed();
    }
}

[TheAspect]
public class BaseClass
{
    
}