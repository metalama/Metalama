// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Workspaces
{
    /// <summary>
    /// Represents a set of workspaces. Two attempts to load a workspace with the same parameters, in the same <see cref="WorkspaceCollection"/>,
    /// will return the exact same instance, unless the <see cref="Reset"/> method is called.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="WorkspaceCollection"/> provides caching of loaded workspaces. When you call <see cref="Load"/> or <see cref="LoadAsync(ImmutableArray{string}, ImmutableDictionary{string, string}?, bool, CancellationToken)"/>
    /// with the same paths and properties, the collection returns the previously loaded <see cref="Workspace"/> instead of reloading it.
    /// This improves performance and ensures that multiple parts of your application can share the same workspace instance.
    /// </para>
    /// <para>
    /// Use the <see cref="Default"/> property to access the default collection, or create a new instance with a custom service provider.
    /// </para>
    /// </remarks>
    /// <seealso cref="Workspace"/>
    /// <seealso href="@introspection-api"/>
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
        /// <param name="serviceProvider">The service provider to use, or <c>null</c> to use the default service provider.</param>
        public WorkspaceCollection( GlobalServiceProvider? serviceProvider = null )
        {
            this.ServiceProvider = serviceProvider ?? ServiceProviderFactory.GetServiceProvider();
        }

        /// <summary>
        /// Gets the default instance of the <see cref="WorkspaceCollection"/> class. This is a singleton instance constructed without
        /// specifying a service provider.
        /// </summary>
        public static WorkspaceCollection Default { get; }

        /// <summary>
        /// Gets the service builder for the current collection. Use this to register custom services that will be available to all workspaces in this collection.
        /// </summary>
        public ServiceBuilder ServiceBuilder { get; } = new();

        /// <summary>
        /// Configures additional services for this collection, resetting any previously registered services, and returns the current collection.
        /// </summary>
        /// <param name="configure">An action that configures services on the <see cref="ServiceBuilder"/>.</param>
        /// <returns>The current <see cref="WorkspaceCollection"/> instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// <strong>Important:</strong> This method must be called <em>before</em> any call to <see cref="Load"/> or <see cref="LoadAsync(string[])"/>.
        /// Services configured after loading have no effect on already-loaded workspaces.
        /// </para>
        /// <para>
        /// This method first clears any previously registered services, then invokes the configure action
        /// to register new services. Services are available for metric computations and other extensibility points.
        /// </para>
        /// <para>
        /// Example usage with metrics:
        /// </para>
        /// <code>
        /// WorkspaceCollection.Default
        ///     .WithServices(s => s.AddMetrics())
        ///     .Load("MySolution.sln")
        ///     .SourceCode
        ///     .Methods
        ///     .Select(m => m.Metrics().Get&lt;SyntaxNodesCount&gt;())
        /// </code>
        /// </remarks>
        /// <seealso cref="ServiceBuilder"/>
        public WorkspaceCollection WithServices( Action<ServiceBuilder> configure )
        {
            this.ServiceBuilder.Clear();
            configure( this.ServiceBuilder );

            return this;
        }

        internal GlobalServiceProvider ServiceProvider { get; }

        [Obsolete( "Errors are now always ignored." )]
        public bool IgnoreLoadErrors { get; set; }

        /// <summary>
        /// Synchronously loads a set of projects or solutions into a <see cref="Workspace"/>, or returns an existing workspace
        /// if the method has been previously called with the exact same parameters.
        /// </summary>
        /// <param name="paths">A list of project or solution paths.</param>
        /// <returns>A <see cref="Workspace"/> where all specified projects or solutions, and their dependencies, have been loaded.</returns>
        /// <seealso cref="LoadAsync(string[])"/>
        public Workspace Load( params string[] paths )
            => this.ServiceProvider.GetRequiredService<ITaskRunner>().RunSynchronously( () => this.LoadAsync( paths.ToImmutableArray() ) );

        /// <summary>
        /// Asynchronously loads a set of projects or solutions into a <see cref="Workspace"/>, or returns an existing workspace
        /// if the method has been previously called with the exact same parameters.
        /// </summary>
        /// <param name="paths">A list of project or solution paths.</param>
        /// <returns>A <see cref="Workspace"/> where all specified projects or solutions, and their dependencies, have been loaded.</returns>
        /// <seealso cref="Load(string[])"/>
        public Task<Workspace> LoadAsync( params string[] paths ) => this.LoadAsync( paths.ToImmutableArray() );

        /// <summary>
        /// Asynchronously loads a set of projects or solutions into a <see cref="Workspace"/>, or returns an existing workspace
        /// if the method has been previously called with the exact same parameters. This overload allows specifying MSBuild properties.
        /// </summary>
        /// <param name="paths">A list of project or solution paths.</param>
        /// <param name="properties">MSBuild properties to pass to the projects, or <c>null</c> to use default properties.</param>
        /// <param name="restore">Indicates whether to run <c>dotnet restore</c> before loading projects. The default is <c>true</c>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Workspace"/> where all specified projects or solutions, and their dependencies, have been loaded.</returns>
        public Task<Workspace> LoadAsync(
            ImmutableArray<string> paths,
            ImmutableDictionary<string, string>? properties = null,
            bool restore = true,
            CancellationToken cancellationToken = default )
        {
            // Expand environment variables and validate paths.
            var expandedPathsBuilder = ImmutableArray.CreateBuilder<string>( paths.Length );

            for ( var i = 0; i < paths.Length; i++ )
            {
                var path = Environment.ExpandEnvironmentVariables( paths[i] );

                if ( string.IsNullOrWhiteSpace( path ) )
                {
                    throw new ArgumentException(
                        $"The path at index {i} is empty or whitespace.",
                        nameof(paths) );
                }

                if ( !File.Exists( path ) )
                {
                    throw new FileNotFoundException(
                        $"The project or solution file was not found: '{path}'",
                        path );
                }

                expandedPathsBuilder.Add( path );
            }

            paths = expandedPathsBuilder.MoveToImmutable();

            properties ??= ImmutableDictionary<string, string>.Empty;
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
        /// Finds the <see cref="Workspace"/> and <see cref="Project"/> that defines a given Roslyn <see cref="Compilation"/> in the current <see cref="WorkspaceCollection"/>.
        /// </summary>
        /// <param name="compilation">The Roslyn compilation to search for.</param>
        /// <param name="workspace">When this method returns <c>true</c>, contains the workspace that contains the compilation.</param>
        /// <param name="project">When this method returns <c>true</c>, contains the project that contains the compilation.</param>
        /// <param name="isMetalamaOutput">When this method returns <c>true</c>, indicates whether the compilation is Metalama's transformed output.</param>
        /// <returns><c>true</c> if a matching project was found; otherwise, <c>false</c>.</returns>
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
        /// Removes all cached workspaces, but not the set of registered services. After calling this method,
        /// subsequent calls to <see cref="Load"/> or <see cref="LoadAsync(ImmutableArray{string}, ImmutableDictionary{string, string}?, bool, CancellationToken)"/> will reload workspaces from disk.
        /// </summary>
        public void Reset()
        {
            // TODO: cancel pending tasks. This is not a critical use case currently.
            this._workspaces.Clear();
        }
    }
}