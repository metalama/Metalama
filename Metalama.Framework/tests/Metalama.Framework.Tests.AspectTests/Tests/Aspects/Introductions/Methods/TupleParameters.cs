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