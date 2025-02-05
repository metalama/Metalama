#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Templating.Aspects.Iterators.DefaultTemplate
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