// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.TemplateTests.Throw.ThrowInRunTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Throw inside a run-time if should NOT terminate compile-time flow
        // because the condition is evaluated at run time.
        [TestTemplate]
        private dynamic? Template()
        {
            // Run-time condition - the throw inside should be serialized but
            // compile-time flow should continue.
            if ( meta.Target.Parameters[0].Value == null )
            {
                throw new ArgumentNullException();
            }

            // This code SHOULD be reached at compile time because the if is run-time.
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
