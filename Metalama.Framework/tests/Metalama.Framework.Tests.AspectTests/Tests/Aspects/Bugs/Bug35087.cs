// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug35087;

public class TestAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @namespace = builder.Advice.WithNamespace(builder.Target.ContainingNamespace, "TestNamespace");
        var type = @namespace.IntroduceClass("TestType");
        type.ImplementInterface(typeof(ITestType));
    }

    [InterfaceMember]
    public void Foo()
    {
    }
}

public interface ITestType
{
    void Foo();
}

// <target>
[TestAspect]
public class Target
{
}