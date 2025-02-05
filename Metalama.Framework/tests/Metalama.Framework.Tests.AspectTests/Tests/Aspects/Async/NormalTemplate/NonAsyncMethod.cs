#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.NormalTemplate.NonAsyncMethod
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
        public Task<int> TaskReturningNonAsync( int a )
        {
            return Task.FromResult( a );
        }

        [Aspect]
        public ValueTask<int> ValueTaskReturningNonAsync( int a )
        {
            return new ValueTask<int>( 0 );
        }

        [Aspect]
        public Task<TResult?> GenericTaskReturningNonAsync<TResult, TInput>( TInput x )
        {
            return Task.FromResult( default(TResult) );
        }

        [Aspect]
        public Task<TResult?> GenericConstraintsTaskReturningNonAsync<TResult, TInput>( TInput x )
            where TResult : IDisposable
            where TInput : IDisposable
        {
            x.Dispose();

            return Task.FromResult( default(TResult) );
        }
    }
}