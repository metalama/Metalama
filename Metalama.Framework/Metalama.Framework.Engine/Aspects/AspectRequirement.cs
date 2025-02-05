// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.Aspects;

internal readonly struct AspectRequirement
{
    public IRef<IDeclaration> TargetDeclaration { get; }

    public IAspectPredecessor Predecessor { get; }

    public AspectRequirement( IRef<IDeclaration> targetDeclaration, IAspectPredecessor predecessor )
    {
        this.TargetDeclaration = targetDeclaration;
        this.Predecessor = predecessor;
    }
}