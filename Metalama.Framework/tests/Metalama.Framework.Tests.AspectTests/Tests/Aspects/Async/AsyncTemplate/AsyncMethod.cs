using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.AsyncMethod
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            return meta.Proceed();
        }

        public override async Task<dynamic?> OverrideAsyncMethod()
        {
            await Task.Yield();
            var result = await meta.Proceed();
            Console.WriteLine( $"result={result}" );

            return result;
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private int NormalMethod( int a )
        {
            return a;
        }

        [Aspect]
        private async Task<int> AsyncTaskResultMethod( int a )
        {
            await Task.Yield();

            return a;
        }

        [Aspect]
        private async Task AsyncTaskMethod()
        {
            await Task.Yield();
        }
    }
}