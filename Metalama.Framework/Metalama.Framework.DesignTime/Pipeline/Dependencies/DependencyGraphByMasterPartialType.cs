// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// Represents the set of syntax trees that depend on a master partial type.
/// </summary>
internal readonly struct DependencyGraphByMasterPartialType
{
    private static readonly ImmutableHashSet<string> _emptyDependencies = ImmutableHashSet.Create<string>().WithComparer( StringComparer.Ordinal );

    public DependencyGraphByMasterPartialType() : this( _emptyDependencies ) { }

    public DependencyGraphByMasterPartialType RemoveDependency( string dependentFilePath ) => new( this.DependentFilePaths.Remove( dependentFilePath ) );

    public ImmutableHashSet<string> DependentFilePaths { get; }

    private DependencyGraphByMasterPartialType( ImmutableHashSet<string> dependentFilePaths )
    {
        this.DependentFilePaths = dependentFilePaths;
    }

    public DependencyGraphByMasterPartialType AddPartialTypeDependency( string dependentFilePath ) => new( this.DependentFilePaths.Add( dependentFilePath ) );
}