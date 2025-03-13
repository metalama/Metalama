// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.ErrorNestedTypeNotFabric
{
    // <target>
    internal class TargetCode
    {
        [CompileTime]
        private enum E { }

        [CompileTime]
        private interface I { }

        [CompileTime]
        private struct S { }

        [CompileTime]
        private delegate void D();

        [CompileTime]
        private record R( int x );

        [CompileTime]
        private class C { }

        [RunTimeOrCompileTime]
        private class C2 { }
    }
}