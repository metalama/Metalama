// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Throw.ThrowInCompileTimeIfInRunTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Throw inside a compile-time if that's inside a run-time if
        // should NOT terminate compile-time flow because we're inside a run-time block.
        [TestTemplate]
        private dynamic? Template()
        {
            // Run-time condition.
            if ( meta.Target.Parameters[0].Value == null )
            {
                // Compile-time condition that is always true.
                if ( meta.Target.Method.Name.Length > 0 )
                {
                    throw new ArgumentNullException();
                }

                // This code should still be processed because we're in a run-time block.
                ThrowIfReached();
            }

            return meta.Proceed();
        }

        [CompileTime]
        private static void ThrowIfReached()
        {
            // This SHOULD be reached because we're inside a run-time if block.
            // The compile-time if just controls which code is emitted.
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method( object? a )
        {
            return null;
        }
    }
}
