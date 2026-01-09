// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.TemplateTests.LocalVariables.TupleDeconstructionConstant
{
    [CompileTime]
    internal class Aspect
    {
        // Test tuple deconstruction with constant/neutral right-hand side
        // Constants are treated as run-time in templates
        [TestTemplate]
        private dynamic? Template()
        {
            // The right-hand side is a constant literal tuple
            (var first, var second) = ("Hello", "World");

            Console.WriteLine( $"{first} {second}" );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        // <target>
        private int Method( int a, int b )
        {
            return a + b;
        }
    }
}
