// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.TupleDeconstructionExplicitVarTest
{
    [CompileTime]
    internal class Aspect
    {
        // Test tuple deconstruction with explicit var for each element: (var x, var y) = tuple;
        [TestTemplate]
        private dynamic? Template()
        {
            if ( meta.Target.Method.Parameters.Count == 0 )
            {
                return null;
            }

            // Tuple deconstruction with TupleExpressionSyntax: (var first, var second) = tuple;
            (var first, var second) = GetTuple();

            return $"{first},{second}";
        }

        [CompileTime]
        private static (string, string) GetTuple()
        {
            return ("a", "b");
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method(int x)
        {
            return null;
        }
    }
}
