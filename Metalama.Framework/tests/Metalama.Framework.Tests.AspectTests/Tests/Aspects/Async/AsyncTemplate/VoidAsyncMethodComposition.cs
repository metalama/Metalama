using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.VoidAsyncMethodComposition;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect1), typeof(Aspect2) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.VoidAsyncMethodComposition
{
    internal class Aspect1 : OverrideMethodAspect
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

    internal class Aspect2 : OverrideMethodAspect
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
        [Aspect1]
        [Aspect2]
        private async void AsyncMethod()
        {
            await Task.Yield();
        }
    }
}