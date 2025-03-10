// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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