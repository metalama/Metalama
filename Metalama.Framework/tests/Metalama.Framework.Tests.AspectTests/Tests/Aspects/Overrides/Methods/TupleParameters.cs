using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.TupleParameters
{
    internal class MyAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException();
        }
    }

    // <target>
    internal class C
    {
        [MyAspect]
        internal void M( (int[] A, (string?, string?[], int?[]) B) c, (int F, (int? G, string? H)? I)? y ) { }
    }
}