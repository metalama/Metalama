// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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