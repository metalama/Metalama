using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.VoidAsyncMethod
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var result = meta.Proceed();
            Console.WriteLine( $"result={result}" );

            return result;
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
        private void NonAsyncMethod() { }

        [Aspect]
        private async void AsyncMethod()
        {
            await Task.Yield();
        }
    }
}