// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Skipped (#34183 - [Template] [InterfaceMember] conflict)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug34183;

public class TestAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.ImplementInterface(typeof(ITest));
        builder.IntroduceMethod(nameof(Foo));
    }

    [InterfaceMember]
    public void Foo()
    {
    }

    [InterfaceMember]
    public void Foo<T>()
    {
    }

    [Template]
    public void Foo<T, U>()
    {
    }
}

public interface ITest
{
    void Foo();
    void Foo<T>();
}

// <target>
[Test]
public class TestClass
{
}