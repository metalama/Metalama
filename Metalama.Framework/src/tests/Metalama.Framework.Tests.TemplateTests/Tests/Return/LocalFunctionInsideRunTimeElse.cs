// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.LocalFunctionInsideRunTimeElse
{
    [CompileTime]
    internal class Aspect
    {
        // Tests local function defined inside a run-time else block.
        [TestTemplate]
        private dynamic? Template()
        {
            var p = meta.Target.Parameters[0];

            // Run-time condition
            if ( p.Value != null )
            {
                return meta.Proceed();
            }
            else
            {
                // Compile-time condition inside run-time else
                if ( meta.Target.Method.Name == "Method" )
                {
                    return LocalFunc( "null input" );

                    return null;
                }

                // Local function inside run-time else
                object? LocalFunc( object? input )
                {
                    global::System.Console.WriteLine( input );

                    return input;
                }
            }

            return null;
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
