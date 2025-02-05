using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Threading.Tasks;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.VariableAssignAsyncTask
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private async Task<dynamic?> Template()
        {
            var result = meta.ProceedAsync();

            return await result;
        }
    }

    internal class TargetCode
    {
        private async Task Method( int a, int b )
        {
            await Task.Yield();
            Console.WriteLine( a / b );
        }
    }
}