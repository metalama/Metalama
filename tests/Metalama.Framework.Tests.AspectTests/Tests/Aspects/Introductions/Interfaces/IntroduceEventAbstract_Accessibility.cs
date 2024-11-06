#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0626

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventAbstract_Accessibility;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        @interface.IntroduceEvent( nameof(TestPublic));
        @interface.IntroduceEvent( nameof(TestInternal));
        @interface.IntroduceEvent( nameof(TestProtected));
        @interface.IntroduceEvent( nameof(TestProtectedInternal));
        @interface.IntroduceEvent( nameof(TestPrivateProtected));
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
#endif