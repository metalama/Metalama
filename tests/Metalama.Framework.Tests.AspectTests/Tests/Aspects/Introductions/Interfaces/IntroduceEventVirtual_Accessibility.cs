using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventVirtual_Accessibility;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var iface = builder.Advice.IntroduceInterface(builder.Target, "ITest");
        builder.Advice.IntroduceProperty(iface.Declaration, nameof(TestPublic));
        builder.Advice.IntroduceProperty(iface.Declaration, nameof(TestInternal));
        builder.Advice.IntroduceProperty(iface.Declaration, nameof(TestProtected));
        builder.Advice.IntroduceProperty(iface.Declaration, nameof(TestProtectedInternal));
        builder.Advice.IntroduceProperty(iface.Declaration, nameof(TestPrivateProtected));
    }

    [Template]
    public int TestPublic
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }
        set
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    internal int TestInternal
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }
        set
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    protected int TestProtected
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }
        set
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    protected internal int TestProtectedInternal
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }
        set
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    private protected int TestPrivateProtected
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }
        set
        {
            Console.WriteLine("Default");
        }
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }