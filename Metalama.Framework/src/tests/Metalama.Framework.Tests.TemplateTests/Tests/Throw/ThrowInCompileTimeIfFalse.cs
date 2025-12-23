// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Throw.ThrowInCompileTimeIfFalse
{
    [CompileTime]
    internal class Aspect
    {
        // When the compile-time condition is false, the throw is not executed
        // and compile-time flow continues.
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile-time condition that is always false.
            if ( meta.Target.Method.Name.Length == 0 )
            {
                throw new InvalidOperationException( "Should not be reached" );
            }

            // This code SHOULD be reached because the condition is false.
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
