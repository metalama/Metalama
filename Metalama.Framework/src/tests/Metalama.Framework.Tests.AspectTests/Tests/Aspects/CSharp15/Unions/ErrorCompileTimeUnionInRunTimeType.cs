// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;

// Verifies the second acceptance criterion of issue #1942: a compile-time union nested in a run-time type is
// reported with the diagnostic that a compile-time struct in the same position reports, rather than being dropped
// silently. The struct is declared beside the union so that the two diagnostics are compared in one baseline. See
// Tests/Fabrics/ErrorNestedTypeNotFabric.cs, which pins the same rule for every other kind of type.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.ErrorCompileTimeUnionInRunTimeType
{
    // The case types are compile-time as well, otherwise the compile-time union is reported for referencing a
    // run-time type before it is reported for its position.

    [CompileTime]
    public record Circle( double Radius );

    [CompileTime]
    public record Rectangle( double Width, double Height );

    // The file needs an aspect, otherwise the compile-time compilation is not built at all.
    public class EmptyAspectAttribute : TypeAspect { }

    // <target>
    [EmptyAspect]
    internal class TargetCode
    {
        [CompileTime]
        private union NestedUnion( Circle, Rectangle );

        [CompileTime]
        private struct NestedStruct { }
    }
}
