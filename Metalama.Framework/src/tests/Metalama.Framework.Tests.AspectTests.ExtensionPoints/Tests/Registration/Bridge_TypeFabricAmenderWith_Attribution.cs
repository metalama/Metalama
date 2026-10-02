// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.ExtensionPoints;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Registration.Bridge_TypeFabricAmenderWith_Attribution;

// The contributions made through the amender of a type fabric, directly or through With, are attributed to the fabric.

// <target>
internal class C
{
    private void M() { }

    private class Fabric : TypeFabric
    {
        public override void AmendType( ITypeAmender amender )
        {
            amender.TestRegister( "amender", this );
            amender.With( amender.Type.Methods.Single() ).TestRegister( "amender-with", this );
        }
    }
}
