#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncIterators.AsyncTemplate
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            throw new NotSupportedException( "Should not be selected" );
        }

        public override async Task<dynamic?> OverrideAsyncMethod()
        {
            await Task.Yield();
            Console.WriteLine( "Before " + meta.Target.Method.Name );
            var result = meta.Proceed();
            Console.WriteLine( "After " + meta.Target.Method.Name );
            await Task.Yield();

            return result;
        }
    }

    internal class Program
    {
        public static async Task Main()
        {
            TargetCode t = new();

            await foreach (var i in t.AsyncEnumerable( 0 ))
            {
                Console.WriteLine( $"  Received {i}" );
            }

            await foreach (var i in t.AsyncEnumerableCancellable( 0, default ))
            {
                Console.WriteLine( $"  Received {i}" );
            }

            await using (var enumerator = t.AsyncEnumerator( 0 ))
            {
                while (await enumerator.MoveNextAsync())
                {
                    Console.WriteLine( $"  Received {enumerator.Current}" );
                }
            }

            await using (var enumerator = t.AsyncEnumeratorCancellable( 0, default ))
            {
                while (await enumerator.MoveNextAsync())
                {
                    Console.WriteLine( $"  Received {enumerator.Current}" );
                }
            }
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        public async IAsyncEnumerable<int> AsyncEnumerable( int a )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            await Task.Yield();
            Console.WriteLine( "Yield 2" );

            yield return 2;

            await Task.Yield();
            Console.WriteLine( "Yield 3" );

            yield return 3;
        }

        [Aspect]
        public async IAsyncEnumerable<int> AsyncEnumerableCancellable( int a, [EnumeratorCancellation] CancellationToken token )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            await Task.Yield();
            Console.WriteLine( "Yield 2" );

            yield return 2;

            await Task.Yield();
            Console.WriteLine( "Yield 3" );

            yield return 3;
        }

        [Aspect]
        public async IAsyncEnumerator<int> AsyncEnumerator( int a )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            await Task.Yield();
            Console.WriteLine( "Yield 2" );

            yield return 2;

            Console.WriteLine( "Yield 3" );
            await Task.Yield();

            yield return 3;
        }

        [Aspect]
        public async IAsyncEnumerator<int> AsyncEnumeratorCancellable( int a, CancellationToken token )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            await Task.Yield();
            Console.WriteLine( "Yield 2" );

            yield return 2;

            Console.WriteLine( "Yield 3" );
            await Task.Yield();

            yield return 3;
        }
    }
}