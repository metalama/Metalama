// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.DesignTime.Rpc;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// Represents a dependency of a compilation to another project.
/// </summary>
internal readonly struct DependencyGraphByDependentProject
{
    private static readonly ImmutableDictionary<DocumentKey, DependencyGraphByMasterSyntaxTree> _emptyDependenciesByMasterDocumentKey =
        ImmutableDictionary<DocumentKey, DependencyGraphByMasterSyntaxTree>.Empty;

    private static readonly ImmutableDictionary<DocumentKey, DependencyCollectorByDependentSyntaxTreeAndMasterProject> _emptyDependenciesByDependentDocumentKey =
        ImmutableDictionary<DocumentKey, DependencyCollectorByDependentSyntaxTreeAndMasterProject>.Empty;

    public ProjectKey ProjectKey { get; }

    /// <summary>
    /// Gets the list of dependencies on syntax trees within the master compilation, indexed by file path.
    /// </summary>
    public ImmutableDictionary<DocumentKey, DependencyGraphByMasterSyntaxTree> DependenciesByMasterDocumentKey { get; }

    public ImmutableDictionary<TypeDependencyKey, DependencyGraphByMasterPartialType> DependenciesByMasterPartialType { get; }

    public bool IsEmpty => this.DependenciesByMasterDocumentKey.Count == 0 && this.DependenciesByMasterPartialType.Count == 0;

    internal ImmutableDictionary<DocumentKey, DependencyCollectorByDependentSyntaxTreeAndMasterProject> DependenciesByDependentDocumentKey { get; }

    public DependencyGraphByDependentProject( ProjectKey projectKey ) : this(
        projectKey,
        _emptyDependenciesByMasterDocumentKey,
        ImmutableDictionary<TypeDependencyKey, DependencyGraphByMasterPartialType>.Empty,
        _emptyDependenciesByDependentDocumentKey ) { }

    private DependencyGraphByDependentProject(
        ProjectKey projectKey,
        ImmutableDictionary<DocumentKey, DependencyGraphByMasterSyntaxTree> dependenciesByMasterDocumentKey,
        ImmutableDictionary<TypeDependencyKey, DependencyGraphByMasterPartialType> dependenciesByMasterPartialType,
        ImmutableDictionary<DocumentKey, DependencyCollectorByDependentSyntaxTreeAndMasterProject> dependenciesByDependentDocumentKey )
    {
        this.ProjectKey = projectKey;
        this.DependenciesByMasterDocumentKey = dependenciesByMasterDocumentKey;
        this.DependenciesByMasterPartialType = dependenciesByMasterPartialType;
        this.DependenciesByDependentDocumentKey = dependenciesByDependentDocumentKey;
    }

    public bool TryRemoveDependentSyntaxTree( DocumentKey dependentDocumentKey, out DependencyGraphByDependentProject newDependenciesGraph )
    {
        if ( !this.DependenciesByDependentDocumentKey.TryGetValue( dependentDocumentKey, out var oldDependencies ) )
        {
            // There is nothing to do because the dependency was not present.
            newDependenciesGraph = this;

            return false;
        }

        // Update syntax tree dependencies.
        var dependenciesByMasterFilePathBuilder = this.DependenciesByMasterDocumentKey.ToBuilder();

        foreach ( var oldMasterFilePathAndHash in oldDependencies.MasterDocumentKeysAndHashes )
        {
            var masterDocumentKey = oldMasterFilePathAndHash.Key;

            if ( dependenciesByMasterFilePathBuilder.TryGetValue( masterDocumentKey, out var syntaxTreeDependencies ) )
            {
                var newSyntaxTreeDependencies = syntaxTreeDependencies.RemoveDependency( dependentDocumentKey );

                if ( newSyntaxTreeDependencies.DependentDocumentKeys.IsEmpty )
                {
                    dependenciesByMasterFilePathBuilder.Remove( masterDocumentKey );
                }
                else
                {
                    dependenciesByMasterFilePathBuilder[masterDocumentKey] = newSyntaxTreeDependencies;
                }
            }
        }

        // Update partial type dependencies.
        var dependenciesByMasterPartialTypesBuilder = this.DependenciesByMasterPartialType.ToBuilder();

        foreach ( var type in oldDependencies.MasterPartialTypes )
        {
            if ( dependenciesByMasterPartialTypesBuilder.TryGetValue( type, out var typeDependencies ) )
            {
                var newTypeDependencies = typeDependencies.RemoveDependency( dependentDocumentKey );

                if ( newTypeDependencies.DependentDocumentKeys.IsEmpty )
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
            this.DependenciesByDependentDocumentKey.Remove( dependentDocumentKey ) );

        return true;
    }

    public bool TryUpdateDependencies(
        DocumentKey dependentDocumentKey,
        DependencyCollectorByDependentSyntaxTreeAndMasterProject dependencies,
        out DependencyGraphByDependentProject newDependenciesGraph )
    {
        // Check if there is any change.
        if ( this.DependenciesByDependentDocumentKey.TryGetValue( dependentDocumentKey, out var oldDependencies )
             && dependencies.IsStructurallyEqual( oldDependencies ) )
        {
            newDependenciesGraph = this;

            return false;
        }

        var dependenciesByMasterFilePathBuilder = this.DependenciesByMasterDocumentKey.ToBuilder();
        var dependenciesByMasterPartialTypeBuilder = this.DependenciesByMasterPartialType.ToBuilder();

        // Add syntax tree dependencies.
        foreach ( var masterFilePathAndHash in dependencies.MasterDocumentKeysAndHashes )
        {
            if ( !dependenciesByMasterFilePathBuilder.TryGetValue( masterFilePathAndHash.Key, out var syntaxTreeDependencies ) )
            {
                syntaxTreeDependencies = new DependencyGraphByMasterSyntaxTree( masterFilePathAndHash.Value );
            }
            else
            {
                syntaxTreeDependencies = syntaxTreeDependencies.UpdateDeclarationHash( masterFilePathAndHash.Value );
            }

            dependenciesByMasterFilePathBuilder[masterFilePathAndHash.Key] = syntaxTreeDependencies.AddSyntaxTreeDependency( dependentDocumentKey );
        }

        // Add partial type dependencies.
        foreach ( var masterPartialType in dependencies.MasterPartialTypes )
        {
            if ( !dependenciesByMasterPartialTypeBuilder.TryGetValue( masterPartialType, out var partialTypeDependencies ) )
            {
                partialTypeDependencies = new DependencyGraphByMasterPartialType();
            }

            dependenciesByMasterPartialTypeBuilder[masterPartialType] = partialTypeDependencies.AddPartialTypeDependency( dependentDocumentKey );
        }

        if ( oldDependencies != null )
        {
            // Remove syntax tree dependencies.
            foreach ( var oldMasterFilePathAndHash in oldDependencies.MasterDocumentKeysAndHashes )
            {
                var masterDocumentKey = oldMasterFilePathAndHash.Key;

                if ( !dependencies.MasterDocumentKeysAndHashes.ContainsKey( masterDocumentKey ) )
                {
                    if ( dependenciesByMasterFilePathBuilder.TryGetValue( masterDocumentKey, out var syntaxTreeDependencies ) )
                    {
                        var newSyntaxTreeDependencies = syntaxTreeDependencies.RemoveDependency( dependentDocumentKey );

                        if ( newSyntaxTreeDependencies.DependentDocumentKeys.IsEmpty )
                        {
                            dependenciesByMasterFilePathBuilder.Remove( masterDocumentKey );
                        }
                        else
                        {
                            dependenciesByMasterFilePathBuilder[masterDocumentKey] = newSyntaxTreeDependencies;
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
                        var newPartialTypeDependencies = partialTypeDependencies.RemoveDependency( dependentDocumentKey );

                        if ( newPartialTypeDependencies.DependentDocumentKeys.IsEmpty )
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
            this.DependenciesByDependentDocumentKey.SetItem( dependentDocumentKey, dependencies ) );

        return true;
    }
}