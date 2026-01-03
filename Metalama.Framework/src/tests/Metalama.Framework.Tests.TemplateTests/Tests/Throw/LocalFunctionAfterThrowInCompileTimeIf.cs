// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Throw.LocalFunctionAfterThrowInCompileTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Local function defined after a throw in a compile-time if should still be generated.
        [TestTemplate]
        private dynamic? Template()
        {
            if ( meta.Target.Method.Name == "Method" )
            {
                throw new InvalidOperationException( LocalFunc( "error" ) );
            }

            return null;

            string LocalFunc( string input )
            {
                return "Error: " + input;
            }
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
