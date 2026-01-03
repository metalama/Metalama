// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInCompileTimeSwitch
{
    [CompileTime]
    internal class Aspect
    {
        // Return inside a compile-time switch case should terminate compile-time flow.
        [TestTemplate]
        private dynamic? Template()
        {
            switch ( meta.Target.Method.Parameters.Count )
            {
                case 0:
                    return null;
                    // Compile-time flow should stop here.

                default:
                    break;
            }

            // This code should NOT be reached when parameter count is 0.
            ThrowIfReached();
            return meta.Proceed();
        }

        [CompileTime]
        private static void ThrowIfReached()
            => throw new InvalidOperationException( "Compile-time flow should have stopped at return." );
    }

    internal class TargetCode
    {
        // <target>
        private object? Method()
        {
            return null;
        }
    }
}
