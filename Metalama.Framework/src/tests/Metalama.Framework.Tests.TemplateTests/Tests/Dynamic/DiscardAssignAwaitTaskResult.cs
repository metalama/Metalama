// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.DiscardAssignAwaitTaskResult
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
        private async Task<int> Method( int a )
        {
            await Task.Yield();

            return a;
        }
    }
}