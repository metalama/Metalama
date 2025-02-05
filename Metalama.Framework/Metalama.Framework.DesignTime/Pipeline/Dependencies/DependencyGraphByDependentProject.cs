// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// Represents a dependency of a compilation to another project.
/// </summary>
internal readonly struct DependencyGraphByDependentProject
{
    private static readonly ImmutableDictionary<string, DependencyGraphByMasterSyntaxTree> _emptyDependenciesByMasterFilePath =
        ImmutableDictionary<string, DependencyGraphByMasterSyntaxTree>.Empty.WithComparers( StringComparer.Ordinal );

    private static readonly ImmutableDictionary<string, DependencyCollectorByDependentSyntaxTreeAndMasterProject> _emptyDependenciesByDependentFilePath =
        ImmutableDictionary<string, DependencyCollectorByDependentSyntaxTreeAndMasterProject>.Empty.WithComparers( StringComparer.Ordinal );

    public ProjectKey ProjectKey { get; }

    /// <summary>
    /// Gets the list of dependencies on syntax trees within the master compilation, indexed by file path.
    /// </summary>
    public ImmutableDictionary<string, DependencyGraphByMasterSyntaxTree> DependenciesByMasterFilePath { get; }

    public ImmutableDictionary<TypeDependencyKey, DependencyGraphByMasterPartialType> DependenciesByMasterPartialType { get; }

    public bool IsEmpty => this.DependenciesByMasterFilePath.Count == 0 && this.DependenciesByMasterPartialType.Count == 0;

    internal ImmutableDictionary<string, DependencyCollectorByDependentSyntaxTreeAndMasterProject> DependenciesByDependentFilePath { get; }

    public DependencyGraphByDependentProject( ProjectKey projectKey ) : this(
        projectKey,
        _emptyDependenciesByMasterFilePath,
        ImmutableDictionary<TypeDependencyKey, DependencyGraphByMasterPartialType>.Empty,
        _emptyDependenciesByDependentFilePath ) { }

    private DependencyGraphByDependentProject(
        ProjectKey projectKey,
        ImmutableDictionary<string, DependencyGraphByMasterSyntaxTree> dependenciesByMasterFilePath,
        ImmutableDictionary<TypeDependencyKey, DependencyGraphByMasterPartialType> dependenciesByMasterPartialType,
        ImmutableDictionary<string, DependencyCollectorByDependentSyntaxTreeAndMasterProject> dependenciesByDependentFilePath )
    {
        this.ProjectKey = projectKey;
        this.DependenciesByMasterFilePath = dependenciesByMasterFilePath;
        this.DependenciesByMasterPartialType = dependenciesByMasterPartialType;
        this.DependenciesByDependentFilePath = dependenciesByDependentFilePath;
    }

    public bool TryRemoveDependentSyntaxTree( string dependentFilePath, out DependencyGraphByDependentProject newDependenciesGraph )
    {
        if ( !this.DependenciesByDependentFilePath.TryGetValue( dependentFilePath, out var oldDependencies ) )
        {
            // There is nothing to do because the dependency was not present.
            newDependenciesGraph = this;

            return false;
        }

        // Update syntax tree dependencies.
        var dependenciesByMasterFilePathBuilder = this.DependenciesByMasterFilePath.ToBuilder();

        foreach ( var oldMasterFilePathAndHash in oldDependencies.MasterFilePathsAndHashes )
        {
            var masterFilePath = oldMasterFilePathAndHash.Key;

            if ( dependenciesByMasterFilePathBuilder.TryGetValue( masterFilePath, out var syntaxTreeDependencies ) )
            {
                var newSyntaxTreeDependencies = syntaxTreeDependencies.RemoveDependency( dependentFilePath );

                if ( newSyntaxTreeDependencies.DependentFilePaths.IsEmpty )
                {
                    dependenciesByMasterFilePathBuilder.Remove( masterFilePath );
                }
                else
                {
                    dependenciesByMasterFilePathBuilder[masterFilePath] = newSyntaxTreeDependencies;
                }
            }
        }

        // Update partial type dependencies.
        var dependenciesByMasterPartialTypesBuilder = this.DependenciesByMasterPartialType.ToBuilder();

        foreach ( var type in oldDependencies.MasterPartialTypes )
        {
            if ( dependenciesByMasterPartialTypesBuilder.TryGetValue( type, out var typeDependencies ) )
            {
                var newTypeDependencies = typeDependencies.RemoveDependency( dependentFilePath );

                if ( newTypeDependencies.DependentFilePaths.IsEmpty )
                {
                    dependenciesByMasterPartialTypesBuilder.Remove( type );
                }
                else
                {
                    dependenciesByMasterPartialTypesBuilder[type] = newTypeDependencies;
                }
            }
        }

        newDependenciesGraph = new DependencyGraphByDependentProject(
            this.ProjectKey,
            dependenciesByMasterFilePathBuilder.ToImmutable(),
            dependenciesByMasterPartialTypesBuilder.ToImmutable(),
            this.DependenciesByDependentFilePath.Remove( dependentFilePath ) );

        return true;
    }

    public bool TryUpdateDependencies(
        string dependentFilePath,
        DependencyCollectorByDependentSyntaxTreeAndMasterProject dependencies,
        out DependencyGraphByDependentProject newDependenciesGraph )
    {
        // Check if there is any change.
        if ( this.DependenciesByDependentFilePath.TryGetValue( dependentFilePath, out var oldDependencies )
             && dependencies.IsStructurallyEqual( oldDependencies ) )
        {
            newDependenciesGraph = this;

            return false;
        }

        var dependenciesByMasterFilePathBuilder = this.DependenciesByMasterFilePath.ToBuilder();
        var dependenciesByMasterPartialTypeBuilder = this.DependenciesByMasterPartialType.ToBuilder();

        // Add syntax tree dependencies.
        foreach ( var masterFilePathAndHash in dependencies.MasterFilePathsAndHashes )
        {
            if ( !dependenciesByMasterFilePathBuilder.TryGetValue( masterFilePathAndHash.Key, out var syntaxTreeDependencies ) )
            {
                syntaxTreeDependencies = new DependencyGraphByMasterSyntaxTree( masterFilePathAndHash.Value );
            }
            else
            {
                syntaxTreeDependencies = syntaxTreeDependencies.UpdateDeclarationHash( masterFilePathAndHash.Value );
            }

            dependenciesByMasterFilePathBuilder[masterFilePathAndHash.Key] = syntaxTreeDependencies.AddSyntaxTreeDependency( dependentFilePath );
        }

        // Add partial type dependencies.
        foreach ( var masterPartialType in dependencies.MasterPartialTypes )
        {
            if ( !dependenciesByMasterPartialTypeBuilder.TryGetValue( masterPartialType, out var partialTypeDependencies ) )
            {
                partialTypeDependencies = new DependencyGraphByMasterPartialType();
            }

            dependenciesByMasterPartialTypeBuilder[masterPartialType] = partialTypeDependencies.AddPartialTypeDependency( dependentFilePath );
        }

        if ( oldDependencies != null )
        {
            // Remove syntax tree dependencies.
            foreach ( var oldMasterFilePathAndHash in oldDependencies.MasterFilePathsAndHashes )
            {
                var masterFilePath = oldMasterFilePathAndHash.Key;

                if ( !dependencies.MasterFilePathsAndHashes.ContainsKey( masterFilePath ) )
                {
                    if ( dependenciesByMasterFilePathBuilder.TryGetValue( masterFilePath, out var syntaxTreeDependencies ) )
                    {
                        var newSyntaxTreeDependencies = syntaxTreeDependencies.RemoveDependency( dependentFilePath );

                        if ( newSyntaxTreeDependencies.DependentFilePaths.IsEmpty )
                        {
                            dependenciesByMasterFilePathBuilder.Remove( masterFilePath );
                        }
                        else
                        {
                            dependenciesByMasterFilePathBuilder[masterFilePath] = newSyntaxTreeDependencies;
                        }
                    }
                }
            }

            // Remove partial types dependencies.

            foreach ( var type in oldDependencies.MasterPartialTypes )
            {
                if ( !dependencies.Contains( type ) )
                {
                    if ( dependenciesByMasterPartialTypeBuilder.TryGetValue( type, out var partialTypeDependencies ) )
                    {
                        var newPartialTypeDependencies = partialTypeDependencies.RemoveDependency( dependentFilePath );

                        if ( newPartialTypeDependencies.DependentFilePaths.IsEmpty )
                        {
                            dependenciesByMasterPartialTypeBuilder.Remove( type );
                        }
                        else
                        {
                            dependenciesByMasterPartialTypeBuilder[type] = newPartialTypeDependencies;
                        }
                    }
                }
            }
        }

        newDependenciesGraph = new DependencyGraphByDependentProject(
            this.ProjectKey,
            dependenciesByMasterFilePathBuilder.ToImmutable(),
            dependenciesByMasterPartialTypeBuilder.ToImmutable(),
            this.DependenciesByDependentFilePath.SetItem( dependentFilePath, dependencies ) );

        return true;
    }
}