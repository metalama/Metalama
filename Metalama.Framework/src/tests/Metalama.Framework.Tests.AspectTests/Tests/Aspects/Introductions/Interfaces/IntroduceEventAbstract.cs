// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0626

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventAbstract;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        var interfaceEvent = @interface.IntroduceEvent( nameof(TestEvent) );

        // Implementation type
        var implementation = builder.IntroduceClass("TestImplementation");
        implementation.ImplementInterface( @interface.Declaration);
        var constructor = implementation.IntroduceConstructor( nameof(Constructor));
        implementation.IntroduceEvent( nameof(TestEventImplementation), buildEvent: b => { b.Name = "TestEvent"; });

        var usage = builder.IntroduceClass("TestUsage");
        usage.IntroduceMethod( nameof(TestUsageMethod), args: new { T = @interface.Declaration, @event = interfaceEvent.Declaration, implementationConstructor = constructor.Declaration });
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
    public T TestUsageMethod<[CompileTime] T>(T instance, [CompileTime] IEvent @event, [CompileTime] IConstructor implementationConstructor)
    {
        @event.With(instance).Add((EventHandler)((s, ea) => { Console.WriteLine("Handler"); }));
        return implementationConstructor.Invoke();
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }