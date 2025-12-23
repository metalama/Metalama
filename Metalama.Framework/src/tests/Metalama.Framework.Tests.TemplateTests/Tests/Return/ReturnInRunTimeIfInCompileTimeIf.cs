// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInRunTimeIfInCompileTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Run-time if nested inside compile-time if:
        // - Compile-time condition is evaluated. If true, the run-time if is emitted.
        // - The compile-time flow should CONTINUE past the compile-time if because
        //   even though compile-time took this branch, the inner run-time if might not execute.
        [TestTemplate]
        private dynamic? Template()
        {
            var p = meta.Target.Parameters[0];

            // Compile-time condition that is always true.
            if ( meta.Target.Method.Name.Length > 0 )
            {
                // Run-time condition - emitted because compile-time condition was true.
                if ( p.Value == null )
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
