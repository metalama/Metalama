// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Invokers;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32906;

[Inheritable]
public sealed class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.Target.Method.DeclaringType.Methods.OfName( "Foo" ).Single().With( InvokerOptions.Base ).Invoke();
        meta.Target.Method.DeclaringType.Methods.OfName( "Bar" ).Single().With( InvokerOptions.Base ).Invoke();
        meta.Target.Method.DeclaringType.Methods.OfName( "Baz" ).Single().With( InvokerOptions.Base ).Invoke();
        meta.Target.Method.DeclaringType.Methods.OfName( "Qux" ).Single().With( InvokerOptions.Base ).Invoke();

        return meta.Proceed();
    }
}

public class BaseClass
{
    public virtual void Foo() { }

    public virtual void Bar() { }

    public void Baz() { }
}

// <target>
public partial class TargetClass : BaseClass
{
    public override void Foo()
    {
        Console.WriteLine( "Override method with no aspect override" );
    }

    public sealed override void Bar()
    {
        Console.WriteLine( "Sealed override method with no aspect override" );
    }

    public new virtual void Baz()
    {
        Console.WriteLine( "Hiding virtual method with no aspect override" );
    }

    public virtual void Qux()
    {
        Console.WriteLine( "Virtual method with no aspect override" );
    }

    [TestAspect]
    public void Test() { }
}