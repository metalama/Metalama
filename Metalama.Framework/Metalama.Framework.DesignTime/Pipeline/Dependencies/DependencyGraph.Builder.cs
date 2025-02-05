// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

internal readonly partial struct DependencyGraph
{
    public struct Builder
    {
        private readonly DependencyGraph _dependencyGraph;
        private ImmutableDictionary<ProjectKey, DependencyGraphByDependentProject>.Builder? _dependenciesByCompilationBuilder;

        public Builder( DependencyGraph dependencyGraph )
        {
            this._dependencyGraph = dependencyGraph;
            this._dependenciesByCompilationBuilder = null;
        }

        private ImmutableDictionary<ProjectKey, DependencyGraphByDependentProject>.Builder GetDependenciesByCompilationBuilder()
            => this._dependenciesByCompilationBuilder ??= this._dependencyGraph.DependenciesByMasterProject.ToBuilder();

        private IReadOnlyDictionary<ProjectKey, DependencyGraphByDependentProject> GetDependenciesByCompilation()
            => (IReadOnlyDictionary<ProjectKey, DependencyGraphByDependentProject>?) this._dependenciesByCompilationBuilder
               ?? this._dependencyGraph.DependenciesByMasterProject;

        public void RemoveDependentSyntaxTree( string path )
        {
            List<DependencyGraphByDependentProject>? modifiedDependencies = null;

            // We need to remove in two stages so we don't modify the collection during enumeration.
            foreach ( var compilationDependencies in this.GetDependenciesByCompilation() )
            {
                if ( compilationDependencies.Value.TryRemoveDependentSyntaxTree( path, out var newDependencies ) )
                {
                    modifiedDependencies ??= new List<DependencyGraphByDependentProject>();
                    modifiedDependencies.Add( newDependencies );
                }
            }

            if ( modifiedDependencies != null )
            {
                foreach ( var dependencyToRemove in modifiedDependencies )
                {
                    if ( dependencyToRemove.IsEmpty )
                    {
                        this.GetDependenciesByCompilationBuilder().Remove( dependencyToRemove.ProjectKey );
                    }
                    else
                    {
                        this.GetDependenciesByCompilationBuilder()[dependencyToRemove.ProjectKey] = dependencyToRemove;
                    }
                }
            }
        }

        public void RemoveProject( ProjectKey projectKey )
        {
            if ( this.GetDependenciesByCompilation().ContainsKey( projectKey ) )
            {
                this.GetDependenciesByCompilationBuilder().Remove( projectKey );
            }
        }

        public void UpdateDependencies(
            ProjectKey projectKey,
            string dependentFilePath,
            DependencyCollectorByDependentSyntaxTreeAndMasterProject dependencies )
        {
            if ( !this.GetDependenciesByCompilation().TryGetValue( projectKey, out var currentDependenciesOfCompilation ) )
            {
                currentDependenciesOfCompilation = new DependencyGraphByDependentProject( projectKey );
            }

            if ( currentDependenciesOfCompilation.TryUpdateDependencies(
                    dependentFilePath,
                    dependencies,
                    out var newDependenciesOfCompilation ) )
            {
                this.GetDependenciesByCompilationBuilder()[projectKey] = newDependenciesOfCompilation;
            }
            else
            {
                // The dependencies have not changed.
            }
        }

        public readonly DependencyGraph ToImmutable()
            => this._dependenciesByCompilationBuilder != null
                ? new DependencyGraph( this._dependenciesByCompilationBuilder.ToImmutable() )
                : this._dependencyGraph;
    }
}