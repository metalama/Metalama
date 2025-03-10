// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline.Dependencies;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

/// <summary>
/// Represents a change in the set of partial types of a <see cref="SyntaxTree"/>. Exposed by <see cref="SyntaxTreeChange"/>.
/// </summary>
internal readonly struct PartialTypeChange
{
    public TypeDependencyKey Type { get; }

    public PartialTypeChangeKind Kind { get; }

    public PartialTypeChange( TypeDependencyKey type, PartialTypeChangeKind kind )
    {
        this.Type = type;
        this.Kind = kind;
    }

    // TODO: Check why we never call Merge.
    // ReSharper disable once UnusedMember.Global

    public PartialTypeChange Merge( PartialTypeChange change )
        => (this.Kind, change.Kind) switch
        {
            (_, PartialTypeChangeKind.None) => this,
            (PartialTypeChangeKind.None, _) => change,
            _ => new PartialTypeChange( this.Type, PartialTypeChangeKind.None )
        };
}