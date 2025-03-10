// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// Represents the set of syntax trees that are dependent on a master syntax tree.
/// </summary>
internal readonly struct DependencyGraphByMasterSyntaxTree
{
    private static readonly ImmutableHashSet<string> _emptyDependencies = ImmutableHashSet.Create<string>().WithComparer( StringComparer.Ordinal );

    /// <summary>
    /// Gets the hash of of the master syntax tree.
    /// </summary>
    public ulong DeclarationHash { get; }

    /// <summary>
    /// Gets the list of dependent syntax trees, by their file path.
    /// </summary>
    public ImmutableHashSet<string> DependentFilePaths { get; }

    public DependencyGraphByMasterSyntaxTree( ulong declarationHash ) : this( declarationHash, _emptyDependencies ) { }

    private DependencyGraphByMasterSyntaxTree( ulong declarationHash, ImmutableHashSet<string> dependentFilePaths )
    {
        this.DeclarationHash = declarationHash;
        this.DependentFilePaths = dependentFilePaths;
    }

    public DependencyGraphByMasterSyntaxTree AddSyntaxTreeDependency( string dependentFilePath )
    {
        if ( this.DependentFilePaths.Contains( dependentFilePath ) )
        {
            return this;
        }
        else
        {
            return new DependencyGraphByMasterSyntaxTree( this.DeclarationHash, this.DependentFilePaths.Add( dependentFilePath ) );
        }
    }

    public DependencyGraphByMasterSyntaxTree RemoveDependency( string dependentFilePath )
    {
        return new DependencyGraphByMasterSyntaxTree( this.DeclarationHash, this.DependentFilePaths.Remove( dependentFilePath ) );
    }

    public DependencyGraphByMasterSyntaxTree UpdateDeclarationHash( ulong hash )
    {
        return new DependencyGraphByMasterSyntaxTree( hash, this.DependentFilePaths );
    }
}