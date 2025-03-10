// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.StaticParameterExclamationMark
{
    internal class Aspect : TypeAspect
    {
        [Introduce]
        private string? Introduced( string? a ) => a!.ToString();
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private class NullableTarget { }

#nullable disable

        [Aspect]
        private class NonNullableTarget { }
    }
}