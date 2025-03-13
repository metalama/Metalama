// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.AssignAwaitTask
{
    internal class Aspect
    {
        [TestTemplate]
        private async Task<dynamic?> Template()
        {
            var x = meta.Default( SpecialType.Int32 );

            x = await meta.ProceedAsync();
            x += await meta.ProceedAsync();
            x *= await meta.ProceedAsync();

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