using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventAbstract_Accessibility;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var iface = builder.Advice.IntroduceInterface(builder.Target, "ITest");
        builder.Advice.IntroduceEvent(iface.Declaration, nameof(TestPublic));
        builder.Advice.IntroduceEvent(iface.Declaration, nameof(TestInternal));
        builder.Advice.IntroduceEvent(iface.Declaration, nameof(TestProtected));
        builder.Advice.IntroduceEvent(iface.Declaration, nameof(TestProtectedInternal));
        builder.Advice.IntroduceEvent(iface.Declaration, nameof(TestPrivateProtected));
    }

    [Template]
    public extern event EventHandler TestPublic;

    [Template]
    internal extern event EventHandler TestInternal;

    [Template]
    protected extern event EventHandler TestProtected;

    [Template]
    protected internal extern event EventHandler TestProtectedInternal;

    [Template]
    private protected extern event EventHandler TestPrivateProtected;
}

// <target>
[IntroductionAttribute]
public class TargetType { }