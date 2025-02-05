using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.CallerAttributes_InlinedNonFinal
{
    /*
     * Tests that overriding operator correctly transforms caller attribute method invocations when the source is inlined into non-final semantic.
     */

    public class OverrideAttribute : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Override( nameof(Override) );
            builder.Override( nameof(Override2) );
        }

        [Template]
        public dynamic? Override()
        {
            Console.WriteLine( "This is the overridden method." );

            return meta.Proceed();
        }

        [Template]
        public dynamic? Override2()
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
        public static int operator -( TargetClass x )
        {
            MethodWithCallerMemberName( 42 );
            MethodWithCallerMemberName( 42, y: 27 );
            MethodWithCallerMemberName( 42, name1: "foo", y: 27 );
            MethodWithCallerMemberName( 42, "foo", 27 );
            MethodWithCallerMemberName( 42, "foo", 27, "bar" );

            return 42;
        }

        [Override]
        public static implicit operator int( TargetClass x )
        {
            MethodWithCallerMemberName( 42 );
            MethodWithCallerMemberName( 42, y: 27 );
            MethodWithCallerMemberName( 42, name1: "foo", y: 27 );
            MethodWithCallerMemberName( 42, "foo", 27 );
            MethodWithCallerMemberName( 42, "foo", 27, "bar" );

            return 42;
        }

        public static void MethodWithCallerMemberName( int x, [CallerMemberName] string name1 = "", int y = 0, [CallerMemberName] string name2 = "" ) { }
    }
}