// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInRunTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Run-time if with return - the if statement and return are both emitted
        // to the output. Compile-time flow continues because the run-time condition
        // might not be true at run-time.
        [TestTemplate]
        private dynamic? Template()
        {
            var p = meta.Target.Parameters[0];

            // Run-time condition - the whole if is emitted to the output.
            if ( p.Value == null )
            {
                return null;
            }

            // This code is always emitted because the run-time condition might be false.
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
