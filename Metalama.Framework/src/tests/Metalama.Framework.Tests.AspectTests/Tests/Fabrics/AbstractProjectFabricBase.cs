// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Fabrics.AbstractProjectFabricBase
{
    // An abstract fabric base class must not be instantiated by the pipeline.
    internal abstract class FabricBase : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender.SelectTypes().SelectMany( t => t.Methods ).AddAspect<Aspect>();
        }
    }

    internal class Fabric : FabricBase { }

    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "overridden" );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( int a ) => a;
    }
}
