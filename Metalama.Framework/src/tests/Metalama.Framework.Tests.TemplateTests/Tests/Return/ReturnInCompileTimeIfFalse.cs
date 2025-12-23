// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInCompileTimeIfFalse
{
    [CompileTime]
    internal class Aspect
    {
        // When the compile-time condition is false, the return should NOT be executed
        // and the code after the if block should run.
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile-time condition that is always false.
            if ( meta.Target.Method.Name.Length == 0 )
            {
                return null;
            }

            // This code SHOULD be reached at compile time when the condition is false.
            return meta.Proceed();
        }
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
