// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Workspaces
{
    /// <summary>
    /// Represents a set of workspaces. Two attempts to load a workspace with the same parameters, in the same <see cref="WorkspaceCollection"/>,
    /// will return the exact same instance, unless the <see cref="Reset"/> method is called.
    /// </summary>
    [PublicAPI]
    public sealed class WorkspaceCollection
    {
        private readonly ConcurrentDictionary<string, Task<Workspace>> _workspaces = new();

        static WorkspaceCollection()
        {
            WorkspaceServices.Initialize();

            // The default collection must be instantiated after the service provider is initialized.
            Default = new WorkspaceCollection();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceCollection"/> class.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public WorkspaceCollection( GlobalServiceProvider? serviceProvider = null )
        {
            this.ServiceProvider = serviceProvider ?? ServiceProviderFactory.GetServiceProvider();
        }

        /// <summary>
        /// Gets the default instance of the <see cref="WorkspaceCollection"/> class. This is a singleton instance constructed without
        /// specifying a service provider.
        /// </summary>
        public static WorkspaceCollection Default { get; }

        public ServiceBuilder ServiceBuilder { get; } = new();

        internal GlobalServiceProvider ServiceProvider { get; }

        [Obsolete( "Errors are now always ignored." )]
        public bool IgnoreLoadErrors { get; set; }

        /// <summary>
        /// Loads a set of projects of solutions into a <see cref="Workspace"/>, or returns an existing workspace
        /// if the method has been previously called with the exact same parameters. 
        /// </summary>
        /// <param name="paths">A list of project or solution paths.</param>
        /// <returns>A <see cref="Workspace"/> where all specified project or solutions, and their dependencies, have been loaded.</returns>
        public Workspace Load( params string[] paths )
            => this.ServiceProvider.GetRequiredService<ITaskRunner>().RunSynchronously( () => this.LoadAsync( paths.ToImmutableArray() ) );

        /// <summary>
        /// Asynchronously loads a set of projects of solutions into a <see cref="Workspace"/>, or returns an existing workspace
        /// if the method has been previously called with the exact same parameters. 
        /// </summary>
        /// <param name="paths">A list of project or solution paths.</param>
        /// <returns>A <see cref="Workspace"/> where all specified project or solutions, and their dependencies, have been loaded.</returns>
        public Task<Workspace> LoadAsync( params string[] paths ) => this.LoadAsync( paths.ToImmutableArray() );

        /// <summary>
        /// Asynchronously loads a set of projects of solutions into a <see cref="Workspace"/>, or returns an existing workspace
        /// if the method has been previously called with the exact same parameters. This overload allows to specify MSBuild properties.
        /// </summary>
        /// <param name="paths">A list of project or solution paths.</param>
        /// <returns>A <see cref="Workspace"/> where all specified project or solutions, and their dependencies, have been loaded.</returns>
        public Task<Workspace> LoadAsync(
            ImmutableArray<string> paths,
            ImmutableDictionary<string, string>? properties = null,
            bool restore = true,
            CancellationToken cancellationToken = default )
        {
            properties ??= properties ?? ImmutableDictionary<string, string>.Empty;
            var key = GetWorkspaceKey( paths, properties );

            async Task<Workspace> LoadCore( string k )
            {
                var workspace = await Workspace.LoadAsync( this.ServiceProvider, key, paths, properties, this, restore, cancellationToken );
                workspace.Disposed += this.OnWorkspaceDisposed;

                return workspace;
            }

            return this._workspaces.GetOrAdd( key, LoadCore );
        }

        private void OnWorkspaceDisposed( object? sender, EventArgs e )
        {
            var workspace = (Workspace) sender!;
            workspace.Disposed += this.OnWorkspaceDisposed;
            this._workspaces.TryRemove( workspace.Key, out _ );
        }

        private static string GetWorkspaceKey( ImmutableArray<string> initialProjects, ImmutableDictionary<string, string> properties )
        {
            var sortedProjects = initialProjects.OrderBy( p => p, StringComparer.Ordinal );
            var sortedProperties = properties.OrderBy( p => p.Key, StringComparer.OrdinalIgnoreCase ).Select( p => $"{p.Key}={p.Value}" );

            return string.Join( ",", sortedProjects.Concat( sortedProperties ) );
        }

        /// <summary>
        /// Finds the <see cref="Workspace"/> and <see cref="Project"/> that defines a given Roslyn <see cref="Compilation" /> in the current <see cref="WorkspaceCollection"/>.
        /// </summary>
        public bool TryFindProject(
            Compilation compilation,
            [NotNullWhen( true )] out Workspace? workspace,
            [NotNullWhen( true )] out Project? project,
            out bool isMetalamaOutput )
        {
            // We are only considering workspaces that have already been loaded.

#pragma warning disable VSTHRD002
            var found = this._workspaces.Values
                .Select(
                    w => w.IsCompleted
                        ? (Project: w.Result.Projects.FirstOrDefault(
                               p => p.RoslynCompilation == compilation
                                    || (p.IsMetalamaOutputEvaluated && p.CompilationResult.TransformedCode.GetRoslynCompilation() == compilation) ),
                           Workspace: w.Result)
                        : (null, null) )
                .FirstOrDefault( p => p.Project != null );
#pragma warning restore VSTHRD002

            if ( found.Project != null )
            {
                workspace = found.Workspace!;
                project = found.Project;
                isMetalamaOutput = project.RoslynCompilation != compilation;

                return true;
            }
            else
            {
                workspace = null;
                project = null;
                isMetalamaOutput = false;

                return false;
            }
        }

        /// <summary>
        /// Removes all cached workspaces, but not the set of registered services.
        /// </summary>
        public void Reset()
        {
            // TODO: cancel pending tasks. This is not a critical use case currently.
            this._workspaces.Clear();
        }
    }
}