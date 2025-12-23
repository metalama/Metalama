// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Throw.ThrowInRunTimeIfInCompileTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Throw inside a run-time if that's inside a compile-time if.
        // The compile-time if controls whether the run-time if is emitted,
        // but the throw inside the run-time if should NOT terminate compile-time flow.
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile-time condition that is always true.
            if ( meta.Target.Method.Name.Length > 0 )
            {
                // Run-time condition.
                if ( meta.Target.Parameters[0].Value == null )
                {
                    throw new ArgumentNullException();
                }

                // This SHOULD be reached because the throw is inside a run-time block.
            }

            return meta.Proceed();
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
