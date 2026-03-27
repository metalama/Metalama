// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.ExceptionInIntroduction
{
    internal class Aspect : TypeAspect
    {
        [Introduce]
        public int Getter => 42 / meta.CompileTime( 0 );

        [Introduce]
        public int Initializer { get; set; } = 42 / meta.CompileTime( 0 );
    }

    // <target>
    [Aspect]
    internal class TargetCode { }
}
