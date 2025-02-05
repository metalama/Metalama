// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// Represents all dependencies of a given compilation.
/// </summary>
internal readonly partial struct DependencyGraph
{
    public static DependencyGraph Create( BaseDependencyCollector dependencies )
    {
        var emptyGraph = new DependencyGraph( ImmutableDictionary<ProjectKey, DependencyGraphByDependentProject>.Empty );

        return emptyGraph.Update( dependencies );
    }

    public static DependencyGraph Empty => new( ImmutableDictionary<ProjectKey, DependencyGraphByDependentProject>.Empty );

    public bool IsUninitialized => this.DependenciesByMasterProject == null!;

    /// <summary>
    /// Gets the dependencies indexed by compilation.
    /// </summary>
    public ImmutableDictionary<ProjectKey, DependencyGraphByDependentProject> DependenciesByMasterProject { get; }

    /// <summary>
    /// Updates the <see cref="DependencyGraph"/> based on a <see cref="BaseDependencyCollector"/>.
    /// </summary>
    public DependencyGraph Update( BaseDependencyCollector dependencyCollector )
    {
        var builder = this.ToBuilder();

        // Add or update dependencies.
        foreach ( var dependenciesByDependentFilePath in dependencyCollector.DependenciesByDependentFilePath )
        {
            var dependentFilePath = dependenciesByDependentFilePath.Key;

            // ReSharper disable once SuspiciousTypeConversion.Global

            foreach ( var dependenciesByMasterProject in dependenciesByDependentFilePath.Value.DependenciesByMasterProject )
            {
                var projectKey = dependenciesByMasterProject.Key;
                builder.UpdateDependencies( projectKey, dependentFilePath, dependenciesByMasterProject.Value );
            }
        }

        // Remove graphs for dependent syntax trees were analyzed but for which no dependency was found.
        foreach ( var syntaxTreeEntry in dependencyCollector.PartialCompilation.SyntaxTrees )
        {
            if ( !dependencyCollector.DependenciesByDependentFilePath.ContainsKey( syntaxTreeEntry.Key ) )
            {
                // The syntax tree does not have any dependency in any compilation.
                builder.RemoveDependentSyntaxTree( syntaxTreeEntry.Key );
            }
        }

        return builder.ToImmutable();
    }

    public Builder ToBuilder() => new( this );

    private DependencyGraph( ImmutableDictionary<ProjectKey, DependencyGraphByDependentProject> dependenciesByMasterProject )
    {
        this.DependenciesByMasterProject = dependenciesByMasterProject;
    }
}