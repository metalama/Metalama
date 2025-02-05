using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.DiscardAssignAwaitTask
{
    internal class Aspect
    {
        [TestTemplate]
        private async Task<dynamic?> Template()
        {
            _ = await meta.ProceedAsync();

            return default;
        }
    }

    internal class TargetCode
    {
        private async Task Method( int a )
        {
            await Task.Yield();
            Console.WriteLine( "Hello, world." );
        }
    }
}