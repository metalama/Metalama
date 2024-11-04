using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventAbstract;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.Advice.IntroduceInterface(builder.Target, "ITest");
        var interfaceEvent = builder.Advice.IntroduceEvent(@interface.Declaration, nameof(TestEvent) );

        // Implementation type
        var implementation = builder.Advice.IntroduceClass(builder.Target, "TestImplementation");
        builder.Advice.ImplementInterface(implementation.Declaration, @interface.Declaration);
        var constructor = builder.Advice.IntroduceConstructor(implementation.Declaration, nameof(Constructor));
        builder.Advice.IntroduceEvent(implementation.Declaration, nameof(TestEventImplementation), buildEvent: b => { b.Name = "TestEvent"; });

        var usage = builder.Advice.IntroduceClass(builder.Target, "TestUsage");
        builder.Advice.IntroduceMethod(usage.Declaration, nameof(TestUsageMethod), args: new { T = @interface.Declaration, @event = interfaceEvent.Declaration, implConstructor = constructor.Declaration });
    }

    [Template]
    public void Constructor()
    {
    }

    [Template]
    public extern event EventHandler TestEvent;

    [Template]
    public event EventHandler TestEventImplementation
    {
        add
        {
            Console.WriteLine("Implementation");
        }

        remove
        {
            Console.WriteLine("Implementation");
        }
    }

    [Template]
    public T TestUsageMethod<[CompileTime] T>(T instance, [CompileTime] IEvent @event, [CompileTime] IConstructor implConstructor)
    {
        @event.Add((EventHandler)((s, ea) => { Console.WriteLine("Handler"); }));
        return implConstructor.Invoke();
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }