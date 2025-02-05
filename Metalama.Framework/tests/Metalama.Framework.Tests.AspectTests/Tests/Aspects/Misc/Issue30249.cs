using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using System.Threading.Tasks;

/*
 * #30249 Async templates do not support Task/void async methods in some expressions
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue30249
{
    internal class MyAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException();
        }

        public override async Task<dynamic?> OverrideAsyncMethod()
        {
            var result = meta.ProceedAsync();

            return await result;
        }
    }

    // <target>
    internal class C
    {
        [MyAspect]
        internal async Task VoidAsyncMethod()
        {
            await Task.Yield();
        }

        [MyAspect]
        internal async Task<int> IntAsyncMethod()
        {
            await Task.Yield();

            return 5;
        }
    }
}