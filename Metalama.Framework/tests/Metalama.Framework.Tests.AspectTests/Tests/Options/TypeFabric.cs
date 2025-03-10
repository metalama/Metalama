// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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