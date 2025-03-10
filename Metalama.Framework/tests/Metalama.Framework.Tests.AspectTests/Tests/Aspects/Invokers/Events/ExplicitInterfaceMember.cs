// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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