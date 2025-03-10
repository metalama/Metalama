// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.AsyncMethod_CrossAssembly
{
    // <target>
    class TargetCode
    {
        [Aspect]
        int NormalMethod(int a)
        {
            return a;
        }
        
        [Aspect]
        async Task<int> AsyncTaskResultMethod(int a)
        {
            await Task.Yield();
            return a;
        }

        [Aspect]
        async Task AsyncTaskMethod()
        {
            await Task.Yield();
        }
    }
}