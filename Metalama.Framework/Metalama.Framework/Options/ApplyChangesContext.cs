// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Options;

/// <summary>
/// Context of an <see cref="IIncrementalObject.ApplyChanges"/> operation.
/// </summary>
[CompileTime]
public readonly struct ApplyChangesContext
{
    /// <summary>
    /// Gets the axis along which the override operation is performed.
    /// </summary>
    public ApplyChangesAxis Axis { get; }

    /// <summary>
    /// Gets the declaration for which the override operation is performed.
    /// </summary>
    public IDeclaration Declaration { get; }

    internal ApplyChangesContext( ApplyChangesAxis axis, IDeclaration declaration )
    {
        this.Axis = axis;
        this.Declaration = declaration;
    }
}