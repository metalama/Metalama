// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.VoidAsyncExpressionBody
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "enter" );

            return meta.Proceed();
        }

        public override async Task<dynamic?> OverrideAsyncMethod()
        {
            Console.WriteLine( "enter" );

            return await meta.ProceedAsync();
        }
    }

    // <target>
    internal class TargetCode
    {
        // Regression test for #1638: an expression-bodied, void-returning async Task method
        // generated CS0030 "Cannot convert type 'void' to 'System.Threading.Tasks.Task'".
        [Aspect]
        private async Task DoVoidAsync() => await Task.Delay( 1 );

        [Aspect]
        private async Task<int> GetValueAsync() => await Task.FromResult( 42 );
    }
}
