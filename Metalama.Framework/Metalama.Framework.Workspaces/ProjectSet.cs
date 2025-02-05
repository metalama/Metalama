// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Workspaces
{
    internal sealed class ProjectSet : IProjectSet
    {
        private readonly ConcurrentDictionary<string, ProjectSet> _subsets = new();
        private readonly CompilationSet _sourceCode;

        /// <summary>
        /// Gets the projects in the current <see cref="ProjectSet"/>.
        /// </summary>
        public ImmutableArray<Project> Projects { get; }

        public ICompilationSet SourceCode => this._sourceCode;

        internal ProjectSet( ImmutableArray<Project> projects, string name )
        {
            this._sourceCode = new CompilationSet( name, projects.AsParallel().Select( x => x.Compilation ).ToImmutableArray() );

            // This gets a snapshot of the collection of project at the moment the object is created.
            this.Projects = projects;
        }

        IProjectSet IProjectSet.GetSubset( Predicate<Project> filter ) => this.GetSubset( filter );

        public ProjectSet GetSubset( Predicate<Project> filter )
        {
            var filteredProjects = this.Projects.Where( p => filter( p ) ).OrderBy( p => p.ToString() ).ToImmutableArray();
            var filteredProjectKey = string.Join( "+", filteredProjects );

            return this._subsets.GetOrAdd(
                filteredProjectKey,
                static ( _, ctx ) => new ProjectSet( ctx.filteredProjects, $"Subset of {ctx.me}" ),
                (me: this, filteredProjects) );
        }

        public IDeclaration GetDeclaration( string projectName, string targetFramework, string declarationId, bool metalamaOutput )
        {
            var projects = this.Projects
                .Where( p => p.Name == projectName && (string.IsNullOrEmpty( targetFramework ) || p.TargetFramework == targetFramework) )
                .ToReadOnlyList();

            switch ( projects.Count )
            {
                case 0:
                    throw new InvalidOperationException( "The current ProjectSet does not contain a project with matching path and target framework." );

                case > 1:
                    throw new InvalidOperationException( "The project targets several frameworks. Specify the target framework." );
            }

            var compilation = metalamaOutput ? projects[0].CompilationResult.TransformedCode : projects[0].Compilation;

            return new SerializableDeclarationId( declarationId ).Resolve( compilation );
        }

        [Memo]
        internal ICompilationSetResult CompilationResult
            => new CompilationSetResult( [..this.Projects.AsParallel().Select( x => x.CompilationResult )], this.ToString() );

        public override string ToString() => this._sourceCode.ToString();

        public ICompilationSet TransformedCode => this.CompilationResult.TransformedCode;

        public ImmutableArray<IIntrospectionAspectLayer> AspectLayers => this.CompilationResult.AspectLayers;

        public ImmutableArray<IIntrospectionAspectInstance> AspectInstances => this.CompilationResult.AspectInstances;

        public ImmutableArray<IIntrospectionAspectClass> AspectClasses => this.CompilationResult.AspectClasses;

        public ImmutableArray<IIntrospectionDiagnostic> Diagnostics => this.CompilationResult.Diagnostics;

        public ImmutableArray<IIntrospectionAdvice> Advice => this.CompilationResult.Advice;

        public ImmutableArray<IIntrospectionTransformation> Transformations => this.CompilationResult.Transformations;

        public bool IsMetalamaEnabled => this.CompilationResult.IsMetalamaEnabled;

        public bool HasMetalamaSucceeded => this.CompilationResult.HasMetalamaSucceeded;
    }
}