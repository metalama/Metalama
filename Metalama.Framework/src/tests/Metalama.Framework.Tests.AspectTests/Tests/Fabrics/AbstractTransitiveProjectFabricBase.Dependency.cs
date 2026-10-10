// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Fabrics.AbstractTransitiveProjectFabricBase
{
    // An abstract transitive fabric base class must not be instantiated by the pipeline of the referencing project.
    public abstract class TransitiveFabricBase : TransitiveProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender.SelectTypes().SelectMany( t => t.Methods ).AddAspect<Aspect>();
        }
    }

    public class TransitiveFabric : TransitiveFabricBase { }

    public class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "overridden" );

            return meta.Proceed();
        }
    }
}
