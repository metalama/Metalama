#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyPrivate;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        var interfacePrivateProperty = @interface.IntroduceProperty( nameof(TestProperty));
        @interface.IntroduceMethod( nameof(TestUsageMethod), args: new { privateProperty = interfacePrivateProperty.Declaration } );

    }

    [Template]
    private int TestProperty
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
    public virtual void TestUsageMethod( [CompileTime] IProperty privateProperty)
    {
        privateProperty.Value = privateProperty.Value + 1;
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif