// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.NestedTupleDeconstructionAfterReturn
{
    [CompileTime]
    internal class Aspect
    {
        // Test nested tuple deconstruction: var (x, (y, z)) = nested;
        [TestTemplate]
        private dynamic? Template()
        {
            if ( meta.Target.Method.Parameters.Count == 0 )
            {
                return null;
            }

            // Nested tuple deconstruction
            var (first, (second, third)) = GetNestedTuple();

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
        private object? Method()
        {
            return null;
        }
    }
}
