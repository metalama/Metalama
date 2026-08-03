// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// Represents the set of syntax trees that depend on a master partial type.
/// </summary>
internal readonly struct DependencyGraphByMasterPartialType
{
    private static readonly ImmutableHashSet<DocumentKey> _emptyDependencies = ImmutableHashSet.Create<DocumentKey>();

    public DependencyGraphByMasterPartialType() : this( _emptyDependencies ) { }

    public DependencyGraphByMasterPartialType RemoveDependency( DocumentKey dependentFilePath ) => new( this.DependentFilePaths.Remove( dependentFilePath ) );

    public ImmutableHashSet<DocumentKey> DependentFilePaths { get; }

    private DependencyGraphByMasterPartialType( ImmutableHashSet<DocumentKey> dependentFilePaths )
    {
        this.DependentFilePaths = dependentFilePaths;
    }

    public DependencyGraphByMasterPartialType AddPartialTypeDependency( DocumentKey dependentFilePath ) => new( this.DependentFilePaths.Add( dependentFilePath ) );
}