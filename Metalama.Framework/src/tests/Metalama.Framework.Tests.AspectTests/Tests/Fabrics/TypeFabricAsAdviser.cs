// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Fabrics.TypeFabricAsAdviser;

// <target>
internal class TargetCode
{
    private class Fabric : TypeFabric
    {
        public override void AmendType( ITypeAmender amender )
        {
            // Use amender directly as IAdviser<INamedType> instead of going through amender.Advice.
            amender.IntroduceMethod( nameof(IntroducedMethod) );
        }

        [Template]
        private void IntroducedMethod()
        {
            Console.WriteLine( "introduced" );
        }
    }
}
