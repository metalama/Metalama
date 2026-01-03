// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInCompileTimeIfInRunTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Compile-time if nested inside run-time if:
        // - The run-time if is emitted to output.
        // - Inside the run-time if, compile-time condition is evaluated.
        // - If compile-time condition is true, the return is emitted inside run-time if.
        // - Compile-time flow should CONTINUE past the run-time if because
        //   the run-time if might not execute at runtime.
        [TestTemplate]
        private dynamic? Template()
        {
            var p = meta.Target.Parameters[0];

            // Run-time condition - always emitted.
            if ( p.Value == null )
            {
                // Compile-time condition that is always true.
                if ( meta.Target.Method.Name.Length > 0 )
                {
                    return null;
                }
            }

            // This code SHOULD be emitted because the run-time if might not execute.
            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method( object? a )
        {
            return a;
        }
    }
}
