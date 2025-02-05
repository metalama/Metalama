using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Simple;
using Metalama.Framework.Code;

#pragma warning disable CS0067

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Simple
{
    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var @event in builder.Target.Events)
            {
                builder.With( @event ).OverrideAccessors( nameof(OverrideAdd), nameof(OverrideRemove) );
            }
        }

        [Template]
        public void OverrideAdd( dynamic value )
        {
            Console.WriteLine( "This is the add template." );
            meta.Proceed();
        }

        [Template]
        public void OverrideRemove( dynamic value )
        {
            Console.WriteLine( "This is the remove template." );
            meta.Proceed();
        }
    }

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public event EventHandler IntroducedEvent
        {
            add
            {
                Console.WriteLine( "This is the introduced add." );
                meta.This.handlers.Add( value );
            }

            remove
            {
                Console.WriteLine( "This is the introduced remove." );
                meta.This.handlers.Remove( value );
            }
        }

        [Introduce]
        public static event EventHandler IntroducedStaticEvent
        {
            add
            {
                Console.WriteLine( "This is the introduced add." );
                meta.ThisType.staticHandlers.Add( value );
            }

            remove
            {
                Console.WriteLine( "This is the introduced remove." );
                meta.ThisType.staticHandlers.Remove( value );
            }
        }
    }

    // <target>
    [Override]
    [Introduction]
    internal class TargetClass
    {
        private HashSet<EventHandler> handlers = new();

        public event EventHandler Event
        {
            add
            {
                Console.WriteLine( "This is the original add." );
                handlers.Add( value );
            }

            remove
            {
                Console.WriteLine( "This is the original remove." );
                handlers.Remove( value );
            }
        }

        private static HashSet<EventHandler> staticHandlers = new();

        public static event EventHandler StaticEvent
        {
            add
            {
                Console.WriteLine( "This is the original add." );
                staticHandlers.Add( value );
            }

            remove
            {
                Console.WriteLine( "This is the original remove." );
                staticHandlers.Remove( value );
            }
        }
    }
}