using System;
using System.ComponentModel;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Overrides.Methods.PartialMethod_AspectAttribute;

/*
 * Tests that overriding partial methods using attributes works.
 */

public class OverrideAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"This is the override of {meta.Target.Method}." );

        return meta.Proceed();
    }
}

// <target>
internal partial class TargetClass
{
    [Override]
    public partial int TargetMethod1();

    public partial int TargetMethod2();

    [Override]
    partial void TargetVoidMethodNoImplementation();

    [Override]
    partial void TargetVoidMethodWithImplementation1();
    
    partial void TargetVoidMethodWithImplementation2();
}

// <target>
internal partial class TargetClass
{
    public partial int TargetMethod1()
    {
        Console.WriteLine( "This is a partial method." );

        return 42;
    }

    [Override]
    public partial int TargetMethod2()
    {
        Console.WriteLine("This is a partial method.");

        return 42;
    }

    partial void TargetVoidMethodWithImplementation1()
    {
        Console.WriteLine( "This is a partial method." );
    }

    [Override]
    partial void TargetVoidMethodWithImplementation2()
    {
        Console.WriteLine("This is a partial method.");
    }
}