// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Engine.Validation;

public readonly struct ValidatorImplementation
{
    public object Implementation { get; }

    public IAspectState? State { get; }

    internal static ValidatorImplementation Create( IAspectPredecessor predecessor )
        => predecessor switch
        {
            IAspectInstance aspectInstance => new ValidatorImplementation( aspectInstance ),
            IFabricInstance fabricInstance => new ValidatorImplementation( fabricInstance.Fabric ),
            _ => throw new AssertionFailedException( $"Unexpected predecessor type: {predecessor.GetType()}." )
        };

    internal static ValidatorImplementation Create( object implementation, IAspectState? aspectState = null ) => new( implementation, aspectState );

    private ValidatorImplementation( IAspectInstance aspectInstance )
    {
        this.Implementation = aspectInstance.Aspect;
        this.State = aspectInstance.AspectState;
    }

    private ValidatorImplementation( Fabric fabric )
    {
        this.Implementation = fabric;
        this.State = null;
    }

    private ValidatorImplementation( object implementation, IAspectState? aspectState )
    {
        this.Implementation = implementation;
        this.State = aspectState;
    }
}