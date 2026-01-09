// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.TemplateTests.LocalVariables.TupleDeconstructionExplicitType
{
    [CompileTime]
    internal class Aspect
    {
        // Test tuple deconstruction with explicit types
        [TestTemplate]
        private dynamic? Template()
        {
            // The right-hand side is compile-time, types are explicit (run-time types)
            (string first, string second) = GetTuple();

            Console.WriteLine( $"{first} {second}" );

            return meta.Proceed();
        }

        [CompileTime]
        private static (string, string) GetTuple()
        {
            return ("Hello", "World");
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
