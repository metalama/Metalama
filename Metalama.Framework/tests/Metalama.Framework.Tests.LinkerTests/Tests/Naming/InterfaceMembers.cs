using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067
#pragma warning disable CS0649

namespace Metalama.Framework.Tests.LinkerTests.Tests.Naming.InterfaceMembers;

interface ITest
{
    int Foo { get; set; }

    int Bar();

    event EventHandler Quz;
}

// <target>
class Target : ITest
{
    int ITest.Foo { get; set; }

    [PseudoOverride(nameof(ITest.Foo), "TestAspect")]
    public int Foo_Override
    {
        get
        {
            return link[_this._cast<ITest>().Foo.set];
        }
        set
        {
            link[_this._cast<ITest>().Foo.set] = value;
        }
    }

    int ITest.Bar()
    {
        return 42;
    }

    [PseudoOverride(nameof(ITest.Bar), "TestAspect")]
    int Bar_Override()
    {
        return link(_this._cast<ITest>().Bar)();
    }

    event EventHandler ITest.Quz
    {
        add { }
        remove { }
    }

    [PseudoOverride(nameof(ITest.Quz), "TestAspect")]
    event EventHandler Quz_Override
    {
        add { link[_this._cast<ITest>().Quz.add] += value; }
        remove { link[_this._cast<ITest>().Quz.remove] -= value; }
    }
}
