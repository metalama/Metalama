using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncIterators.AsyncTemplateOnNonAsync
{
    internal class Aspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Override( new MethodTemplateSelector( nameof(OverrideMethod), nameof(OverrideAsyncMethod), useAsyncTemplateForAnyAwaitable: true ) );
        }

        [Template]
        public dynamic OverrideMethod()
        {
            throw new NotSupportedException( "Should not be selected" );
        }

        [Template]
        public async Task<dynamic?> OverrideAsyncMethod()
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
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        public IAsyncEnumerable<int> AsyncEnumerable( int a )
        {
            Console.WriteLine( "Not Async" );

            return AsyncEnumerableImpl( a );
        }

        private async IAsyncEnumerable<int> AsyncEnumerableImpl( int a )
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
    }
}