#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
#endif

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.CrossAssembly_AsyncEnumerable
{
    // <target>
    [Override]
    [Introduction]
    internal class TargetClass
    {
        public async IAsyncEnumerable<int> ExistingMethod_AsyncIterator()
        {
            Console.WriteLine("Original");
            await Task.Yield();
            yield return 42;
        }
    }
}