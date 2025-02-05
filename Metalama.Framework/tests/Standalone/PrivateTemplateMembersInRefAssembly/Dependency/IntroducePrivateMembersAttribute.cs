using Metalama.Framework.Aspects;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo( "Tests" )]

namespace Dependency;

public class IntroducePrivateMembersAttribute : OverrideMethodAspect
{
    [Introduce]
    private readonly RunTimeOnlyClass _field;

    [Introduce]
    private void Method(RunTimeOnlyClass arg) {}

    [Introduce]
    private RunTimeOnlyClass AutoProperty { get; set; }

    [Introduce]
    internal RunTimeOnlyClass Property { get => new(); private set {} }

    [Introduce]
    private event EventHandler<RunTimeOnlyClass> FieldLikeEvent;

    [Introduce]
    private event EventHandler<RunTimeOnlyClass> Event { add {} remove {} }
    
    public override dynamic OverrideMethod()
    {
        Method(_field);

        Console.WriteLine(AutoProperty);

        Console.WriteLine(Property);

        FieldLikeEvent += (s, e) => Console.WriteLine(e);

        Event += (s, e) => Console.WriteLine(e);
        
        return meta.Proceed();
    }
}

internal class RunTimeOnlyClass : EventArgs { }