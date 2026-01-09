// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.TupleMixedDeconstructionTest
{
    [CompileTime]
    internal class Aspect
    {
        // Test mixed tuple deconstruction: (var first, var (second, third))
        // This combines TupleExpression syntax with nested ParenthesizedVariableDesignation
        [TestTemplate]
        private dynamic? Template()
        {
            if ( meta.Target.Method.Parameters.Count == 0 )
            {
                return null;
            }

            // Mixed tuple deconstruction: (var first, var (second, third)) = tuple;
            (var first, var (second, third)) = GetNestedTuple();

            return $"{first},{second},{third}";
        }

        [CompileTime]
        private static (string, (string, string)) GetNestedTuple()
        {
            return ("a", ("b", "c"));
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
