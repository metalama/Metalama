#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodPrivate;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        var interfacePrivateMethod = @interface.IntroduceMethod( nameof(TestMethod));
        @interface.IntroduceMethod( nameof(TestUsageMethod), args: new { privateMethod = interfacePrivateMethod.Declaration } );

    }

    [Template]
    private void TestMethod()
    {
        Console.WriteLine("Default");
    }

    [Template]
    public virtual void TestUsageMethod( [CompileTime] IMethod privateMethod)
    {
        privateMethod.Invoke();
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif