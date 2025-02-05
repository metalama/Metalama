#pragma warning disable CS0067

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.CopyAttributes_CrossAssembly
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceEvent(
                "IntroducedEvent",
                nameof(Template),
                nameof(Template),
                args: new { x = 42 } );
        }

        [Introduce]
        [Foo( 1 )]
        public event EventHandler? Event
        {
            [return: Foo( 2 )]
            [param: Foo( 3 )]
            [Foo( 4 )]
            add
            {
                Console.WriteLine( "Original add accessor." );
            }

            [return: Foo( 5 )]
            [param: Foo( 6 )]
            [Foo( 7 )]
            remove
            {
                Console.WriteLine( "Original remove accessor." );
            }
        }

        [Introduce]
        [Foo( 1 )]
        [method: Foo( 2 )]

        // Backing field of event fields are not currently supported as there is no symbols for them.
        //[field: Foo(3)]
        public event EventHandler? FieldLikeEvent;

        [Foo( 1 )]
        [return: Foo( 2 )]
        [Template]
        public void Template( [CompileTime] int x, [Foo( 3 )] EventHandler? y )
        {
            y?.Invoke( null, new EventArgs() );
        }
    }

    public class FooAttribute : Attribute
    {
        public FooAttribute( int z ) { }
    }
}