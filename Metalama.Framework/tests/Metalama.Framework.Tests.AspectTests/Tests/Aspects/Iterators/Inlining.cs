#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

// This is to test that there is actually no inlining, because it would break.

namespace Metalama.Framework.Tests.AspectTests.Templating.Aspects.Iterators.Inlining
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<dynamic?> OverrideEnumerableMethod()
        {
            Console.WriteLine( "Begin " + meta.Target.Method.Name );
            var x = meta.ProceedEnumerable();

            return x;
        }

        public override IEnumerator<dynamic?> OverrideEnumeratorMethod()
        {
            Console.WriteLine( "Begin " + meta.Target.Method.Name );

            return meta.ProceedEnumerator();
        }
    }

    internal class Program
    {
        private static void TestMain()
        {
            TargetCode targetCode = new();

            foreach (var a in targetCode.Enumerable( 0 ))
            {
                Console.WriteLine( $" Received {a}" );
            }

            foreach (var a in targetCode.OldEnumerable( 0 ))
            {
                Console.WriteLine( $" Received {a}" );
            }

            var enumerator1 = targetCode.Enumerator( 0 );

            while (enumerator1.MoveNext())
            {
                Console.WriteLine( $" Received {enumerator1.Current}" );
            }

            var enumerator2 = targetCode.OldEnumerator( 0 );

            while (enumerator2.MoveNext())
            {
                Console.WriteLine( $" Received {enumerator2.Current}" );
            }
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        public IEnumerable<int> Enumerable( int a )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            Console.WriteLine( "Yield 2" );

            yield return 2;

            Console.WriteLine( "Yield 3" );

            yield return 3;
        }

        [Aspect]
        public IEnumerator<int> Enumerator( int a )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            Console.WriteLine( "Yield 2" );

            yield return 2;

            Console.WriteLine( "Yield 3" );

            yield return 3;
        }

        [Aspect]
        public IEnumerable OldEnumerable( int a )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            Console.WriteLine( "Yield 2" );

            yield return 2;

            Console.WriteLine( "Yield 3" );

            yield return 3;
        }

        [Aspect]
        public IEnumerator OldEnumerator( int a )
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