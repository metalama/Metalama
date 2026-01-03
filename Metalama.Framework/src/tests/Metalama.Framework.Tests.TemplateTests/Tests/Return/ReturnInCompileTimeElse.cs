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

namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInCompileTimeElse
{
    [CompileTime]
    internal class Aspect
    {
        // When the compile-time condition is false, the else branch executes
        // and the return in the else block should stop the compile-time flow.
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile-time condition that is always false.
            if ( meta.Target.Method.Name.Length == 0 )
            {
                return meta.Proceed();
            }
            else
            {
                return null;
                // Compile-time flow should stop here.
            }

            // This code should NOT be reached at compile time.
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
