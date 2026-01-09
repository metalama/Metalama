// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.OutVarInIfAfterReturn
{
    [CompileTime]
    internal class Aspect
    {
        // Test that out variables declared in an if statement after a compile-time return
        // are properly handled with Unsafe.SkipInit
        [TestTemplate]
        private dynamic? Template()
        {
            if ( meta.Target.Method.Parameters.Count == 0 )
            {
                return null;
            }

            // This out variable is declared in an if statement
            if ( TryGetValue( out var value ) )
            {
                return value;
            }

            return meta.Proceed();
        }

        [CompileTime]
        private static bool TryGetValue( out object? value )
        {
            value = null;
            return false;
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
