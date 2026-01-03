// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.LocalFunctionAfterReturnInCompileTimeIfInRunTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Tests local function after return in compile-time if that's inside a run-time if.
        // The local function should be generated because compile-time flow continues past
        // the run-time if (since it might not execute at runtime).
        // The return null; after the compile-time return should NOT appear because
        // the skip flag should work within the compile-time conditional.
        [TestTemplate]
        private dynamic? Template()
        {
            var p = meta.Target.Parameters[0];

            // Run-time condition - always emitted.
            if ( p.Value != null )
            {
                // Compile-time condition that is always true.
                if ( meta.Target.Method.Name == "Method" )
                {
                    return LocalFunc( meta.Proceed() );

                    // This return should NOT be emitted - skip flag should work
                    return null;
                }
            }

            // This code SHOULD be emitted because the run-time if might not execute.
            return meta.Proceed();

            object? LocalFunc( object? input )
            {
                global::System.Console.WriteLine( "LocalFunc called" );

                return input;
            }
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
