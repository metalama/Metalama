using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_Programmatic;

/*
 * Simple case with explicit interface members for a single interface, implemented programmatically.
 */

public interface IInterface
{
    int InterfaceMethod( int i );

    event EventHandler Event;

    event EventHandler? EventField;

    int Property { get; set; }

    int AutoProperty { get; set; }
}

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var explicitImplementation = builder.ImplementInterface( typeof(IInterface) ).ExplicitMembers;

        explicitImplementation.IntroduceMethod( nameof(InterfaceMethod) );
        explicitImplementation.IntroduceEvent( nameof(Event) );
        explicitImplementation.IntroduceEvent( nameof(EventField) );
        explicitImplementation.IntroduceProperty( nameof(Property) );
        explicitImplementation.IntroduceProperty( nameof(AutoProperty) );
    }

    [Template]
    public int InterfaceMethod( int i )
    {
        Console.WriteLine( "This is introduced interface member." );

        return i;
    }

    [Template]
    public event EventHandler? Event
    {
        add
        {
            Console.WriteLine( "This is introduced interface member." );
        }

        remove
        {
            Console.WriteLine( "This is introduced interface member." );
        }
    }

    [Template]
    public event EventHandler? EventField;

    [Template]
    public int Property
    {
        get
        {
            Console.WriteLine( "This is introduced interface member." );

            return 42;
        }

        set
        {
            Console.WriteLine( "This is introduced interface member." );
        }
    }

    [Template]
    public int AutoProperty { get; set; }
}

// <target>
[Introduction]
public class TargetClass { }