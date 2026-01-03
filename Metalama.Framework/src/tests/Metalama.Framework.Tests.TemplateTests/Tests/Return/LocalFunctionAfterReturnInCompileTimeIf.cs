// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.LocalFunctionAfterReturnInCompileTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Bug #1269: When a compile-time conditional with an early return references a local function
        // that is defined after the conditional, the local function must still be generated.
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile-time condition that is always true.
            if ( meta.Target.Method.Name.Length > 0 )
            {
                // This return references LocalFunc which is defined below.
                return LocalFunc( meta.Proceed() );
            }

            // Unreachable code when condition is true, but the local function must still be generated
            // because it's referenced in the return above.
            return null;

            object? LocalFunc( object? input )
            {
                Console.WriteLine( "LocalFunc called" );
                return input;
            }
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method()
        {
            return "test";
        }
    }
}
