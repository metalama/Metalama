// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

// Verifies issue #2150: LAMA0229 is reported for every kind of [RunTimeOrCompileTime] type nested in a run-time type,
// not only for classes.

namespace Metalama.Framework.Tests.AspectTests.Tests.Fabrics.ErrorNestedRunTimeOrCompileTimeType
{
    // The file needs an aspect, otherwise the compile-time compilation is not built at all.
    public class EmptyAspectAttribute : TypeAspect { }

    // <target>
    [EmptyAspect]
    internal class TargetCode
    {
        [RunTimeOrCompileTime]
        private enum E { }

        [RunTimeOrCompileTime]
        private interface I { }

        [RunTimeOrCompileTime]
        private struct S { }

        [RunTimeOrCompileTime]
        private delegate void D();

        [RunTimeOrCompileTime]
        private record R( int x );

        [RunTimeOrCompileTime]
        private record struct RS( int x );

        [RunTimeOrCompileTime]
        private class C { }
    }
}
