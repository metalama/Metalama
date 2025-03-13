// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Threading.Tasks;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.ReturnStatements.ReturnAsyncAwaitTaskResultProceed
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private async Task<dynamic?> Template()
        {
            return await meta.ProceedAsync();
        }
    }

    internal class TargetCode
    {
        // <target>
        private async Task<int> Method( int a, int b )
        {
            await Task.Yield();
            Console.WriteLine( a / b );

            return 1;
        }
    }
}