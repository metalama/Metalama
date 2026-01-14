// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.LocalFunctionInsideNestedCompileTimeConditions
{
    [CompileTime]
    internal class Aspect
    {
        // Tests local function defined inside nested compile-time conditions with early return.
        // The local function should be generated because it's used in the return statement.
        [TestTemplate]
        private dynamic? Template()
        {
            // Outer compile-time condition that is always true.
            if ( meta.Target.Method.Name.Length > 0 )
            {
                // Inner compile-time condition that is also always true.
                if ( meta.Target.Method.Parameters.Count >= 0 )
                {
                    return LocalFunc( meta.Proceed() );

                    // Local function defined inside nested compile-time ifs
                    object? LocalFunc( object? input )
                    {
                        global::System.Console.WriteLine( "LocalFunc called" );

                        return input;
                    }
                }
            }

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method( object? x )
        {
            return x;
        }
    }
}
