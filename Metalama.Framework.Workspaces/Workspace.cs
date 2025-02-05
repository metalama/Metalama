// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Introspection;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILogger = Metalama.Backstage.Diagnostics.ILogger;

namespace Metalama.Framework.Workspaces
{
    /// <summary>
    /// Represents a set of projects. Workspaces can be created using the <see cref="WorkspaceCollection"/> class.  When projects target several frameworks,
    /// they are represented by several instances of the <see cref="Project"/> class in the workspace.
    /// </summary>
    [PublicAPI]
    public sealed class Workspace : IDisposable, IProjectSet, IWorkspaceLoadInfo
    {
        private static readonly ILogger _logger;

        private readonly WorkspaceCollection _collection;
        private readonly CompileTimeDomain _domain;
        private readonly IntrospectionOptionsBox _introspectionOptions;

        internal string Key { get; }

        private readonly ITaskRunner _taskRunner;
        private ProjectSet _filteredProjects;
        private ProjectSet _unfilteredProjects;
        private ImmutableList<WorkspaceDiagnostic> _loadDiagnostics;
        private static readonly Predicate<Project> _defaultProjectFilter = _ => true;
        private Predicate<Project> _projectFilter = _defaultProjectFilter;

        static Workspace()
        {
            WorkspaceServices.Initialize();
            _logger = BackstageServiceFactory.ServiceProvider.GetLoggerFactory().GetLogger( "Workspace" );
        }

        /// <summary>
        /// Loads a set of projects of solutions into a <see cref="Workspace"/>, or returns an existing workspace
        /// if the method has been previously called with the exact same parameters.
        /// This method creates the workspace in the default <see cref="WorkspaceCollection"/>.
        /// </summary>
        /// <param name="paths">A list of project or solution paths.</param>
        /// <returns>A <see cref="Workspace"/> where all specified project or solutions, and their dependencies, have been loaded.</returns>
        public static Workspace Load( params string[] paths ) => WorkspaceCollection.Default.Load( paths );

        /// <summary>
        /// Asynchronously loads a set of projects of solutions into a <see cref="Workspace"/>, or returns an existing workspace
        /// if the method has been previously called with the exact same parameters.
        /// This method creates the workspace in the default <see cref="WorkspaceCollection"/>. 
        /// </summary>
        /// <param name="paths">A list of project or solution paths.</param>
        /// <returns>A <see cref="Workspace"/> where all specified project or solutions, and their dependencies, have been loaded.</returns>
        public static Task<Workspace> LoadAsync( params string[] paths ) => WorkspaceCollection.Default.LoadAsync( paths );

        private Workspace(
            GlobalServiceProvider serviceProvider,
            ImmutableArray<string> loadedPaths,
            ImmutableDictionary<string, string>? properties,
            string key,
            LoadProjectSetResult projectSet,
            WorkspaceCollection collection,
            CompileTimeDomain domain,
            IntrospectionOptionsBox introspectionOptions )
        {
            this.Properties = properties ?? ImmutableDictionary<string, string>.Empty;
            this.LoadedPaths = loadedPaths;
            this.Key = key;
            this._filteredProjects = this._unfilteredProjects = projectSet.Projects;
            this._loadDiagnostics = projectSet.LoadDiagnostics;
            this._collection = collection;
            this._domain = domain;
            this._introspectionOptions = introspectionOptions;
            this._taskRunner = serviceProvider.GetRequiredService<ITaskRunner>();
        }

        /// <summary>
        /// Gets the <see cref="IntrospectionOptions"/> for the current workspace.
        /// </summary>
        public IntrospectionOptions IntrospectionOptions
        {
            get => this._introspectionOptions.IntrospectionOptions;
            private set => this._introspectionOptions.IntrospectionOptions = value;
        }

        /// <summary>
        /// Modifies the <see cref="Introspection.IntrospectionOptions"/> of the current workspace, and returns the current workspace.
        /// </summary>
        public Workspace WithIntrospectionOptions( IntrospectionOptions options )
        {
            this.IntrospectionOptions = options;

            return this;
        }

        /// <summary>
        /// Modifies the <see cref="IntrospectionOptions"/> of the current workspace by setting the <see cref="Introspection.IntrospectionOptions.IgnoreErrors"/>
        /// property to <c>true</c>.
        /// </summary>
        /// <returns></returns>
        public Workspace WithIgnoreErrors()
        {
            // ReSharper disable once WithExpressionModifiesAllMembers
            this.IntrospectionOptions = this.IntrospectionOptions with { IgnoreErrors = true };

            return this;
        }

        /// <summary>
        /// Clear all filters applied by <see cref="ApplyFilter"/>.
        /// </summary>
        public void ClearFilters()
        {
            this._projectFilter = _defaultProjectFilter;
            this._filteredProjects = this._unfilteredProjects;
        }

        /// <summary>
        /// Filters the <see cref="Projects"/> collection with a given predicate.
        /// This allows to filter the output of methods such as <see cref="DeclarationExtensions.GetInboundReferences"/>
        /// or <see cref="DeclarationExtensions.GetDerivedTypes"/> to the filtered subset.
        /// </summary>
        /// <seealso cref="ClearFilters"/>
        public void ApplyFilter( Predicate<Project> filter )
        {
            var oldFilter = this._projectFilter;
            this._projectFilter = p => oldFilter( p ) && filter( p );
            this._filteredProjects = this._unfilteredProjects.GetSubset( this._projectFilter );
        }

        /// <summary>
        /// Reloads all projects in the current workspace.
        /// </summary>
        public async Task<Workspace> ReloadAsync( bool restore = true, CancellationToken cancellationToken = default )
        {
            var result = await LoadProjectSetAsync(
                this.LoadedPaths,
                this.Properties,
                this._collection,
                this._domain,
                this._introspectionOptions,
                restore,
                new Future<Workspace>() { Value = this },
                cancellationToken );

            this._unfilteredProjects = result.Projects;
            this._filteredProjects = this._unfilteredProjects.GetSubset( this._projectFilter );
            this._loadDiagnostics = result.LoadDiagnostics;

            return this;
        }

        public Workspace Reload( bool restore = true, CancellationToken cancellationToken = default )
        {
            this._taskRunner.RunSynchronously( () => this.ReloadAsync( restore, cancellationToken ) );

            return this;
        }

        internal static async Task<Workspace> LoadAsync(
            GlobalServiceProvider serviceProvider,
            string key,
            ImmutableArray<string> projects,
            ImmutableDictionary<string, string> properties,
            WorkspaceCollection collection,
            bool restore,
            CancellationToken cancellationToken )
        {
            var domain = new UnloadableCompileTimeDomain( serviceProvider );

            var future = new Future<Workspace>();
            var introspectionOptions = new IntrospectionOptionsBox();
            var result = await LoadProjectSetAsync( projects, properties, collection, domain, introspectionOptions, restore, future, cancellationToken );

            future.Value = new Workspace(
                serviceProvider,
                projects,
                properties,
                key,
                result,
                collection,
                domain,
                introspectionOptions );

            return future.Value;
        }

        private static void DotNetRestore( GlobalServiceProvider serviceProvider, string project )
        {
            var dotNetTool = new DotNetTool( serviceProvider );
            dotNetTool.Execute( $"restore \"{Path.GetFileName( project )}\"", Path.GetDirectoryName( Path.GetFullPath( project ) ) );
        }

        private record LoadProjectSetResult( ProjectSet Projects, ImmutableList<WorkspaceDiagnostic> LoadDiagnostics );

        private static Task<LoadProjectSetResult> LoadProjectSetAsync(
            ImmutableArray<string> projects,
            ImmutableDictionary<string, string> properties,
            WorkspaceCollection collection,
            CompileTimeDomain domain,
            IIntrospectionOptionsProvider introspectionOptions,
            bool restore,
            Future<Workspace> workspace,
            CancellationToken cancellationToken )
        {
            if ( projects.IsEmpty )
            {
                return Task.FromResult(
                    new LoadProjectSetResult( new ProjectSet( ImmutableArray<Project>.Empty, "Empty" ), ImmutableList<WorkspaceDiagnostic>.Empty ) );
            }

            // We need to initialize MSBuild once per process. The initialization may depend on `global.json`.
            // In case we are using a single process to analyze projects with repos with different global.json,
            // weird things may appear. Currently this case is not covered.
            if ( !MSBuildLocator.IsRegistered )
            {
                MSBuildInitializer.Initialize( Path.GetDirectoryName( Path.GetFullPath( projects[0] ) )! );
            }

            // We can call the next method only after MSBuild initialization because it loads MSBuild assemblies.
            return LoadProjectSetCoreAsync( projects, properties, collection, domain, introspectionOptions, restore, workspace, cancellationToken );
        }

        private static async Task<LoadProjectSetResult> LoadProjectSetCoreAsync(
            ImmutableArray<string> projects,
            ImmutableDictionary<string, string> properties,
            WorkspaceCollection collection,
            CompileTimeDomain domain,
            IIntrospectionOptionsProvider introspectionOptions,
            bool restore,
            Future<Workspace> workspace,
            CancellationToken cancellationToken )
        {
            var allProperties = properties
                .Add( "MSBuildEnableWorkloadResolver", "false" );

            var roslynWorkspace = MSBuildWorkspace.Create( allProperties );

            string? name = null;

            foreach ( var path in projects )
            {
                switch ( Path.GetExtension( path ).ToLowerInvariant() )
                {
                    case ".csproj":
                        if ( restore )
                        {
                            DotNetRestore( collection.ServiceProvider, path );
                        }

                        await roslynWorkspace.OpenProjectAsync( path, cancellationToken: cancellationToken );

                        if ( projects.Length == 1 )
                        {
                            name = $"{Path.GetFileName( path )} and dependencies";
                        }

                        break;

                    case ".sln":
                    case ".slnf":
                        if ( restore )
                        {
                            DotNetRestore( collection.ServiceProvider, path );
                        }

                        await roslynWorkspace.OpenSolutionAsync( path, cancellationToken: cancellationToken );

                        name = $"{Path.GetFileName( path )}";

                        break;

                    default:
                        throw new ArgumentOutOfRangeException( nameof(path), "Invalid path extension. Only '.csproj', '.sln' and '.slnf' are allowed." );
                }
            }

            using var msbuildProjectCollection = new ProjectCollection( allProperties );
            msbuildProjectCollection.RegisterLogger( new MSBuildLogger( _logger ) );

            // Start all tasks in parallel because even that may be expensive.
            var loadProjectTasks = roslynWorkspace.CurrentSolution.Projects.AsParallel().Select( GetOurProjectAsync ).ToArray();

            var ourProjects = (await Task.WhenAll( loadProjectTasks )).ToImmutableArray();

            var projectSet = new ProjectSet( ourProjects, name ?? $"{ourProjects.Length} projects" );

            // Throw an exception upon failure because otherwise it's too difficult to diagnose.
            var diagnostics = roslynWorkspace.Diagnostics.ToReadOnlyList();

            if ( diagnostics.Any() )
            {
                // Log,
                foreach ( var diagnostic in roslynWorkspace.Diagnostics )
                {
                    (diagnostic.Kind == WorkspaceDiagnosticKind.Failure ? _logger.Error : _logger.Warning)?.Log( diagnostic.Message );
                }

                foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
                {
                    _logger.Trace?.Log( $"Loaded assembly: '{assembly}' from '{assembly.Location}'." );
                }
            }

            return new LoadProjectSetResult( projectSet, roslynWorkspace.Diagnostics );

            async Task<Project> GetOurProjectAsync( Microsoft.CodeAnalysis.Project roslynProject )
            {
                // Get an evaluated MSBuild project (the Roslyn workspace presumably does but the result is not made available). 
                var targetFramework = WorkspaceProjectOptions.GetTargetFrameworkFromRoslynProject( roslynProject );

                Dictionary<string, string>? projectProperties = null;

                if ( targetFramework != null )
                {
                    projectProperties = new Dictionary<string, string> { ["TargetFramework"] = targetFramework };
                }

                // ReSharper disable once AccessToDisposedClosure
                var msbuildProject = msbuildProjectCollection.LoadProject( roslynProject.FilePath!, projectProperties, null );

                // Gets a Roslyn compilation.
                var compilation = (await roslynProject.GetCompilationAsync( cancellationToken )).AssertNotNull();

                // Merge service injection.
                var existingAdditionalServiceCollection = collection.ServiceProvider.GetService<AdditionalServiceCollection>();

                var additionalServiceCollection = new AdditionalServiceCollection( existingAdditionalServiceCollection );
                additionalServiceCollection.ProjectServices.Add( collection.ServiceBuilder.Build );

                // Create a compilation model.
                var projectOptions = new WorkspaceProjectOptions( roslynProject, msbuildProject, compilation );

                var projectServiceProvider = collection.ServiceProvider.Underlying
                    .WithService( additionalServiceCollection, true )
                    .WithProjectScopedServices( projectOptions, compilation )
                    .WithService( _ => new WorkspaceIntrospectionService( workspace ) );

                // Create our workspace project.
                var ourProject = new Project(
                    projectServiceProvider,
                    roslynProject.FilePath!,
                    compilation,
                    projectOptions,
                    introspectionOptions );

                return ourProject;
            }
        }

        public event EventHandler? Disposed;

        /// <inheritdoc />
        public void Dispose()
        {
            this._domain.Dispose();

            this.Disposed?.Invoke( this, EventArgs.Empty );
        }

        /// <inheritdoc />
        public ImmutableArray<Project> Projects => this._filteredProjects.Projects;

        /// <inheritdoc />
        public ICompilationSet SourceCode => this._filteredProjects.SourceCode;

        /// <inheritdoc />
        public ImmutableArray<string> LoadedPaths { get; }

        /// <inheritdoc />
        public ImmutableDictionary<string, string> Properties { get; }

        /// <inheritdoc />
        public IProjectSet GetSubset( Predicate<Project> filter ) => this._filteredProjects.GetSubset( filter );

        /// <inheritdoc />
        public IDeclaration GetDeclaration( string projectName, string targetFramework, string declarationId, bool metalamaOutput )
            => this._filteredProjects.GetDeclaration( projectName, targetFramework, declarationId, metalamaOutput );

        internal ICompilationSetResult CompilationResult => this._filteredProjects.CompilationResult;

        /// <inheritdoc />
        public ICompilationSet TransformedCode => this.CompilationResult.TransformedCode;

        /// <inheritdoc />
        public ImmutableArray<IIntrospectionAspectLayer> AspectLayers => this.CompilationResult.AspectLayers;

        /// <inheritdoc />
        public ImmutableArray<IIntrospectionAspectInstance> AspectInstances => this.CompilationResult.AspectInstances;

        /// <inheritdoc />
        public ImmutableArray<IIntrospectionAspectClass> AspectClasses => this.CompilationResult.AspectClasses;

        /// <inheritdoc />
        public ImmutableArray<IIntrospectionAdvice> Advice => this.CompilationResult.Advice;

        /// <inheritdoc />
        public ImmutableArray<IIntrospectionTransformation> Transformations => this.CompilationResult.Transformations;

        /// <inheritdoc />
        public bool IsMetalamaEnabled => this.CompilationResult.IsMetalamaEnabled;

        /// <inheritdoc />
        public bool HasMetalamaSucceeded => this.CompilationResult.HasMetalamaSucceeded;

        /// <inheritdoc />
        public ImmutableArray<IIntrospectionDiagnostic> Diagnostics => this.CompilationResult.Diagnostics;

        public ImmutableArray<IIntrospectionDiagnostic> WorkspaceDiagnostics
            => this._loadDiagnostics.SelectAsImmutableArray( x => (IIntrospectionDiagnostic) new WorkspaceDiagnosticWrapper( x ) );

#pragma warning disable CA1822

        /// <summary>
        /// Gets the version number of Metalama. This is determined by the LinqPad packages for Metalama, not by the Metalama packages in the projects
        /// loaded in the workspace. 
        /// </summary>
        public string? MetalamaVersion => EngineAssemblyMetadataReader.Instance.PackageVersion;

        public Project GetProject( string name, string? targetFramework = null )
        {
            var candidates = this.Projects.Where( p => p.Name == name && (targetFramework == null || p.TargetFramework == targetFramework) ).ToReadOnlyList();

            return candidates.Count switch
            {
                0 => throw new KeyNotFoundException(),
                > 1 => throw new InvalidOperationException( "Ambiguous match." ),
                _ => candidates[0]
            };
        }

#pragma warning restore CA1822
    }
}