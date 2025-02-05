using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.CallerAttributes
{
    /*
     * Tests that overriding event does correctly transform caller attribute method invocations when the source is not inlined.
     */

    public class OverrideAttribute : EventAspect
    {
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.OverrideAccessors( nameof(Override), nameof(Override) );
        }

        [Template]
        public dynamic? Override()
        {
            // Block inlining.
            _ = meta.Proceed();
            Console.WriteLine( "This is the overridden method." );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public event EventHandler OverriddenEvent
        {
            add
            {
                MethodWithCallerMemberName( 42 );
                MethodWithCallerMemberName( 42, y: 27 );
                MethodWithCallerMemberName( 42, name1: "foo", y: 27 );
                MethodWithCallerMemberName( 42, "foo", 27 );
                MethodWithCallerMemberName( 42, "foo", 27, "bar" );
            }

            remove
            {
                MethodWithCallerMemberName( 42 );
                MethodWithCallerMemberName( 42, y: 27 );
                MethodWithCallerMemberName( 42, name1: "foo", y: 27 );
                MethodWithCallerMemberName( 42, "foo", 27 );
                MethodWithCallerMemberName( 42, "foo", 27, "bar" );
            }
        }

        public void MethodWithCallerMemberName( int x, [CallerMemberName] string name1 = "", int y = 0, [CallerMemberName] string name2 = "" ) { }
    }
}