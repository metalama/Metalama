// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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