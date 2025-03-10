// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.NormalTemplate.AsyncMethod_CrossAssembly
{
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
        async Task MethodReturningTask( int a )
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
        async ValueTask MethodReturningValueTask( int a )
        {
            await Task.Yield();
            Console.WriteLine("Oops");
        }
    }
}