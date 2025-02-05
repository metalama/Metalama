#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.NormalTemplate.AsyncMethod
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Before" );
            var result = meta.Proceed();
            Console.WriteLine( "After" );

            return result;
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private async Task<int> MethodReturningTaskOfInt( int a )
        {
            await Task.Yield();

            return a;
        }

        [Aspect]
        private async Task MethodReturningTask( int a )
        {
            await Task.Yield();
            Console.WriteLine( "Oops" );
        }

        [Aspect]
        private async ValueTask<int> MethodReturningValueTaskOfInt( int a )
        {
            await Task.Yield();

            return a;
        }

        [Aspect]
        private async ValueTask MethodReturningValueTask( int a )
        {
            await Task.Yield();
            Console.WriteLine( "Oops" );
        }
    }
}