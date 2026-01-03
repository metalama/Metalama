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

namespace Metalama.Framework.Tests.TemplateTests.Return.LocalFunctionReferencingVariableWithEarlyReturn
{
    [CompileTime]
    internal class Aspect
    {
        // Edge case: Local function references a variable declared before it,
        // and there's an early return in a compile-time conditional.
        [TestTemplate]
        private dynamic? Template()
        {
            var message = "Hello";

            if ( meta.Target.Method.Name.Length > 0 )
            {
                return LocalFunc( meta.Proceed() );
            }

            return null;

            object? LocalFunc( object? input )
            {
                Console.WriteLine( message );
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
