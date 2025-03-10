// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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