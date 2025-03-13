// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.TupleParameters
{
    internal class MyAspect : TypeAspect
    {
        [Introduce]
        internal void M( (int[] A, (string? C, string?[] D, int?[] E) B) x, (int F, (int? G, string? H)? I)? y )
        {
            // Check that the names have persisted.
            Console.WriteLine( $"{x.A}, {x.B.C}, {y!.Value.F}" );
        }
    }

#nullable disable

    // <target>
    [MyAspect]
    internal class C { }
}