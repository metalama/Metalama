// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Throw.ThrowExpressions
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Run-time
            object? a = null;
            var b = a == null ? 1 : throw new Exception();
            var c = a ?? throw new Exception();

            return null;
        }
    }

    internal class TargetCode
    {
        private void Method( int a )
        {
            Console.WriteLine( "Hello, world." );
        }
    }
}