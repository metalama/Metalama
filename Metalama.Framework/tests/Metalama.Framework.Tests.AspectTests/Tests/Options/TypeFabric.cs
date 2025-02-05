// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
#if TEST_OPTIONS
// @Include(_Common.cs)
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.TypeFabric_
{
    // <target>
    [ShowOptionsAspect]
    public class C1
    {
        [ShowOptionsAspect]
        public void M( [ShowOptionsAspect] int p ) { }

        private class Fabric : TypeFabric
        {
            public override void AmendType( ITypeAmender amender )
            {
                amender.SetOptions( c => new MyOptions { Value = "Type" } );
            }
        }
    }
}