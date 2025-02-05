using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33214;

[Inheritable]
public sealed class TestContract : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        Console.WriteLine( "Should be applied only on Foo(int) parameter." );
    }
}

[Inheritable]
public sealed class TestOverride : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Should be applied only on Bar(int) method." );

        return meta.Proceed();
    }
}

public interface TestInterface
{
    void Foo();

    void Foo( [TestContract] int value );

    void Bar();

    [TestOverride]
    void Bar( int value );
}

// <target>
public class TestClass : TestInterface
{
    public void Foo() { }

    public void Foo( int value ) { }

    public void Bar() { }

    public void Bar( int value ) { }
}