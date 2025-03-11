// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.ExplicitInterfaceMember;

[Inheritable]
public sealed class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        _ = meta.Target.Type.Properties.Single().Value;
        meta.Target.Type.Properties.Single().Value = 42;

        return meta.Proceed();
    }
}

public interface ITestInterface
{
    int Bar { get; set; }
}

// <target>
public partial class TestClass : ITestInterface
{
    int ITestInterface.Bar { get; set; }

    [TestAspect]
    public void Foo() { }
}