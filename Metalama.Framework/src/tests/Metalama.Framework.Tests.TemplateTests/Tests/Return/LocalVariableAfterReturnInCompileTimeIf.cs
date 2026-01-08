// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.LocalVariableAfterReturnInCompileTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Bug: When a local variable is declared after a return in a compile-time if,
        // the variable declaration gets wrapped in if (__skip) but subsequent usage
        // is also wrapped, making the variable invisible.
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile-time condition with a return that makes subsequent code conditional
            if ( meta.Target.Method.Name.Length < 0 )
            {
                return null;
            }

            // COMPILE-TIME variable declaration - the bug is that this gets wrapped in `if (!__skip)`
            var method = meta.Target.Method;

            // This tries to use the compile-time variable, but it's in a separate `if (!__skip)` block
            // so the variable is not visible here, causing a compilation error
            method.Invoke();

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
