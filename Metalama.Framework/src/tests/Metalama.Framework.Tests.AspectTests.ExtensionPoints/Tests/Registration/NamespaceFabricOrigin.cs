// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.ExtensionPoints;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Registration.NamespaceFabricOrigin;

// A registration made through the query of a namespace fabric is attributed to the fabric.

internal class Fabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender ) => amender.SelectTypes().TestRegisterQuery( "namespace" );
}

// <target>
internal class C { }
