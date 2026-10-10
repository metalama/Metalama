// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Fabrics.AbstractProjectFabricBase_ParameterizedConstructor
{
    // An abstract fabric base class without a default constructor must not be instantiated by the pipeline.
    internal abstract class FabricBase : ProjectFabric
    {
        private readonly string _message;

        protected FabricBase( string message )
        {
            this._message = message;
        }

        public override void AmendProject( IProjectAmender amender )
        {
            amender.SelectTypes().SelectMany( t => t.Methods ).AddAspect( _ => new Aspect( this._message ) );
        }
    }

    internal class Fabric : FabricBase
    {
        public Fabric() : base( "overridden" ) { }
    }

    internal class Aspect : OverrideMethodAspect
    {
        private readonly string _message;

        public Aspect( string message )
        {
            this._message = message;
        }

        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( this._message );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( int a ) => a;
    }
}
