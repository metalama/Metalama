#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventPrivate;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        var interfacePrivateEvent = @interface.IntroduceEvent( nameof(TestEvent));
        @interface.IntroduceMethod( nameof(TestUsageMethod), args: new { privateEvent = interfacePrivateEvent.Declaration } );

    }

    [Template]
    private event EventHandler TestEvent
    {
        add
        {
            Console.WriteLine("Default");
        }

        remove
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    public virtual void TestUsageMethod( [CompileTime] IEvent privateEvent)
    {
        privateEvent.Add((EventHandler)((s, ea) => { Console.WriteLine("Handler"); }));
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif