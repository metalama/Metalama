using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.ExplicitInterfaceMember;

[Inheritable]
public sealed class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.Target.Type.Events.Single().Add( null );
        meta.Target.Type.Events.Single().Remove( null );

        return meta.Proceed();
    }
}

public interface ITestInterface
{
    event EventHandler? Bar;
}

// <target>
public partial class TestClass : ITestInterface
{
    event EventHandler? ITestInterface.Bar
    {
        add { }
        remove { }
    }

    [TestAspect]
    public void Foo() { }
}