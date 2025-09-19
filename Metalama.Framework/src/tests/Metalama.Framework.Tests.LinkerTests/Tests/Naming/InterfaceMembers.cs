// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067
#pragma warning disable CS0649

namespace Metalama.Framework.Tests.LinkerTests.Tests.Naming.InterfaceMembers;

internal interface ITest
{
    int Foo { get; set; }

    int Bar();

    event EventHandler Quz;
}

// <target>
internal class Target : ITest
{
    int ITest.Foo { get; set; }

    [PseudoOverride(nameof(ITest.Foo), "TestAspect")]
    public int Foo_Override
    {
        get
        {
            return Link[This.Cast<ITest>().Foo.set];
        }
        set
        {
            Link[This.Cast<ITest>().Foo.set] = value;
        }
    }

    int ITest.Bar()
    {
        return 42;
    }

    [PseudoOverride(nameof(ITest.Bar), "TestAspect")]
    private int Bar_Override()
    {
        return Link(This.Cast<ITest>().Bar)();
    }

    event EventHandler ITest.Quz
    {
        add { }
        remove { }
    }

    [PseudoOverride(nameof(ITest.Quz), "TestAspect")]
    private event EventHandler Quz_Override
    {
        add { Link[This.Cast<ITest>().Quz.add] += value; }
        remove { Link[This.Cast<ITest>().Quz.remove] -= value; }
    }
}
