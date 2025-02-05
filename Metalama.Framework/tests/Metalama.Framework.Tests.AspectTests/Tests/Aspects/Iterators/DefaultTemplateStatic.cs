#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Templating.Aspects.Iterators.DefaultTemplateStatic
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( $"Before {meta.Target.Member.Name}" );
            var result = meta.Proceed();
            Console.WriteLine( $"After {meta.Target.Member.Name}" );

            return result;
        }
    }

    internal class Program
    {
        private static void TestMain()
        {
            foreach (var a in TargetCode.Enumerable( 0 ))
            {
                Console.WriteLine( $" Received {a}" );
            }

            foreach (var a in TargetCode.OldEnumerable( 0 ))
            {
                Console.WriteLine( $" Received {a}" );
            }

            var enumerator1 = TargetCode.Enumerator( 0 );

            while (enumerator1.MoveNext())
            {
                Console.WriteLine( $" Received {enumerator1.Current}" );
            }

            var enumerator2 = TargetCode.OldEnumerator( 0 );

            while (enumerator2.MoveNext())
            {
                Console.WriteLine( $" Received {enumerator2.Current}" );
            }
        }
    }

    // <target>
    internal static class TargetCode
    {
        [Aspect]
        public static IEnumerable<int> Enumerable( int a )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            Console.WriteLine( "Yield 2" );

            yield return 2;

            Console.WriteLine( "Yield 3" );

            yield return 3;
        }

        [Aspect]
        public static IEnumerator<int> Enumerator( int a )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            Console.WriteLine( "Yield 2" );

            yield return 2;

            Console.WriteLine( "Yield 3" );

            yield return 3;
        }

        [Aspect]
        public static IEnumerable OldEnumerable( int a )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            Console.WriteLine( "Yield 2" );

            yield return 2;

            Console.WriteLine( "Yield 3" );

            yield return 3;
        }

        [Aspect]
        public static IEnumerator OldEnumerator( int a )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            Console.WriteLine( "Yield 2" );

            yield return 2;

            Console.WriteLine( "Yield 3" );

            yield return 3;
        }
    }
}