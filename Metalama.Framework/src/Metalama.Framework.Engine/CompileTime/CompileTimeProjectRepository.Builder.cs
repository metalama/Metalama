// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating.Mapping;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed partial class CompileTimeProjectRepository
{
    /// <summary>
    /// Returns a new <see cref="CompileTimeProjectRepository"/>.
    /// </summary>
    public static CompileTimeProjectRepository? Create(
        CompileTimeDomain domain,
        ProjectServiceProvider serviceProvider,
        Compilation compilation,
        IDiagnosticAdder? diagnostics = null,
        bool cacheOnly = false,
        IReadOnlyList<SyntaxTree>? compileTimeTreesHint = null,
        CancellationToken cancellationToken = default )
    {
        diagnostics ??= ThrowingDiagnosticAdder.Instance;

        var builder = new Builder( domain, serviceProvider, compilation );

        if ( !builder.TryBuild( compilation, compileTimeTreesHint, diagnostics, cacheOnly, cancellationToken, out var repository ) )
        {
            return null;
        }

        return repository;
    }

    // This class is made internal for tests only.
    internal sealed class Builder
    {
        private readonly CompileTimeCompilationBuilder _builder;
        private readonly CompileTimeProject _frameworkProject;
        private readonly ILogger _logger;

        private readonly ProjectServiceProvider _serviceProvider;

        // The dictionary may contain null values when the assembly does not reference Metalama.Framework.
        private readonly Dictionary<AssemblyIdentity, CompileTimeProject?> _projects = new();
        private readonly CompileTimeDomain _domain;
        private readonly IAssemblyLocator _runTimeAssemblyLocator;
        private readonly CacheableTemplateDiscoveryContextProvider _cacheableTemplateDiscoveryContextProvider;
        private readonly ClassifyingCompilationContextFactory _classifyingCompilationContextFactory;

        private static Compilation CreateEmptyCompilation( in ProjectServiceProvider serviceProvider )
        {
            var assemblyLocator = serviceProvider.GetReferenceAssemblyLocator();

            return CSharpCompilation.Create( "empty", references: assemblyLocator.MetadataReferences );
        }

        // This constructor is used in tests.
        public Builder(
            CompileTimeDomain domain,
            ProjectServiceProvider serviceProvider ) : this( domain, serviceProvider, CreateEmptyCompilation( serviceProvider ) ) { }

        public Builder(
            CompileTimeDomain domain,
            ProjectServiceProvider serviceProvider,
            Compilation compilation )
        {
            this._serviceProvider = serviceProvider;
            this._cacheableTemplateDiscoveryContextProvider = new CacheableTemplateDiscoveryContextProvider( compilation, serviceProvider );

            this._classifyingCompilationContextFactory = serviceProvider.GetRequiredService<ClassifyingCompilationContextFactory>();

            this._runTimeAssemblyLocator = serviceProvider.GetRequiredService<IAssemblyLocator>();
            this._domain = domain;
            this._logger = serviceProvider.GetLoggerFactory().CompileTime();

            this._frameworkProject = serviceProvider.Global.GetRequiredService<FrameworkCompileTimeProjectFactory>()
                .CreateFrameworkProject( serviceProvider, domain, compilation );

            this._projects.Add( this._frameworkProject.RunTimeIdentity, this._frameworkProject );
            this._builder = new CompileTimeCompilationBuilder( serviceProvider, domain );
        }

        public bool TryBuild(
            Compilation compilation,
            IReadOnlyList<SyntaxTree>? compileTimeTreesHint,
            IDiagnosticAdder diagnosticSink,
            bool cacheOnly,
            CancellationToken cancellationToken,
            out CompileTimeProjectRepository? loader )
        {
            var compilationContext = this._classifyingCompilationContextFactory.GetInstance( compilation );

            if ( !this.TryGetCompileTimeProjectFromCompilation(
                    compilationContext,
                    compileTimeTreesHint,
                    diagnosticSink,
                    cacheOnly,
                    cancellationToken,
                    out var compileTimeProject ) )
            {
                loader = null;

                return false;
            }

            if ( compileTimeProject == null )
            {
                throw new AssertionFailedException( $"Metalama is not enabled for the project '{compilation.AssemblyName}'." );
            }

            this.ReportAmbiguousCompileTimeAssemblyNames( compileTimeProject, diagnosticSink );

            loader = new CompileTimeProjectRepository(
                this._domain,
                this._serviceProvider,
                this._projects,
                compileTimeProject );

            return true;
        }

        /// <summary>
        /// Reports a warning for every compile-time assembly name that is claimed by more than one project of the
        /// closure of <paramref name="rootProject"/>, naming the run-time assemblies of all the projects that claim it.
        /// </summary>
        /// <remarks>
        /// This is the only place where the whole closure is known and a diagnostic sink is available. Without it,
        /// the condition surfaces much later, as a failed lookup by compile-time assembly name (typically while
        /// serializing a type name), from where the offending references can no longer be named. See #1749.
        /// </remarks>
        internal void ReportAmbiguousCompileTimeAssemblyNames( CompileTimeProject rootProject, IDiagnosticAdder diagnosticSink )
        {
            foreach ( var group in rootProject.ClosureProjectsGroupedByCompileTimeAssemblyName )
            {
                if ( group.Value.Count <= 1 )
                {
                    continue;
                }

                var runTimeAssemblies = string.Join( ", ", group.Value.SelectAsReadOnlyList( p => $"'{p.RunTimeIdentity}'" ) );

                this._logger.Warning?.Log( $"Several compile-time projects have the compile-time assembly name '{group.Key}': {runTimeAssemblies}." );

                diagnosticSink.Report(
                    GeneralDiagnosticDescriptors.DuplicateCompileTimeAssemblyName.CreateRoslynDiagnostic(
                        null,
                        (group.Key, runTimeAssemblies) ) );
            }
        }

        /// <summary>
        /// Registers the compile-time project of a run-time assembly identity, and returns the project that is
        /// registered for that identity afterwards.
        /// </summary>
        /// <remarks>
        /// Resolving the references of a project can register the identity of that project before the project itself
        /// is registered, so a plain <c>Add</c> threw <c>ArgumentException: An item with the same key has already been
        /// added</c> (issue #1749). The instance registered first wins, because it is the one already handed to other
        /// callers, so that a single compile-time projection is used for a given run-time assembly.
        /// </remarks>
        private CompileTimeProject? RegisterProject( AssemblyIdentity runTimeIdentity, CompileTimeProject? project )
        {
            if ( !this._projects.TryGetValue( runTimeIdentity, out var registeredProject ) )
            {
                this._projects.Add( runTimeIdentity, project );

                return project;
            }

            if ( registeredProject == null )
            {
                // The assembly was known to have no compile-time code, so anything we resolved since then is better.
                this._projects[runTimeIdentity] = project;

                return project;
            }

            if ( !ReferenceEquals( registeredProject, project ) )
            {
                this._logger.Warning?.Log(
                    $"Two compile-time projects were resolved for '{runTimeIdentity}': keeping '{registeredProject}' and discarding '{project}'." );
            }

            return registeredProject;
        }

        // This method is only used in tests.
        internal bool TryGetCompileTimeProjectFromCompilation(
            Compilation compilation,
            IReadOnlyList<SyntaxTree>? compileTimeTreesHint,
            IDiagnosticAdder diagnosticSink,
            bool cacheOnly,
            CancellationToken cancellationToken,
            out CompileTimeProject? compileTimeProject )
            => this.TryGetCompileTimeProjectFromCompilation(
                this._classifyingCompilationContextFactory.GetInstance( compilation ),
                compileTimeTreesHint,
                diagnosticSink,
                cacheOnly,
                cancellationToken,
                out compileTimeProject );

        /// <summary>
        /// Generates a <see cref="CompileTimeProject"/> for a given run-time <see cref="Compilation"/>.
        /// Referenced projects are loaded or generated as necessary. Note that other methods of this class do not
        /// generate projects, they will only ones that have been generated or loaded by this method.
        /// </summary>
        private bool TryGetCompileTimeProjectFromCompilation(
            ClassifyingCompilationContext compilationContext,
            IReadOnlyList<SyntaxTree>? compileTimeTreesHint,
            IDiagnosticAdder diagnosticSink,
            bool cacheOnly,
            CancellationToken cancellationToken,
            out CompileTimeProject? compileTimeProject )
        {
            var runTimeCompilation = compilationContext.SourceCompilation;

            if ( this._projects.TryGetValue( runTimeCompilation.Assembly.Identity, out compileTimeProject ) )
            {
                return true;
            }

            this._logger.Trace?.Log( $"TryGetCompileTimeProjectFromCompilation('{compilationContext.SourceCompilation.AssemblyName}')" );

            List<CompileTimeProject> referencedProjects = [this._frameworkProject];

            foreach ( var reference in runTimeCompilation.References )
            {
                this._logger.Trace?.Log( $"Considering reference '{reference.Display}'." );

                if ( this.TryGetCompileTimeProject(
                        reference,
                        diagnosticSink,
                        cacheOnly,
                        cancellationToken,
                        out var referencedProject ) )
                {
                    if ( referencedProject != null )
                    {
                        this._logger.Trace?.Log( $"Adding a compile-time reference: '{reference.Display}'." );
                        referencedProjects.Add( referencedProject );
                    }
                    else
                    {
                        this._logger.Trace?.Log( $"Not a compile-time reference: '{reference.Display}'." );
                    }
                }
                else
                {
                    // Coverage: ignore
                    // (this happens when the project reference could not be resolved.)

                    this._logger.Warning?.Log(
                        $"The project reference from '{runTimeCompilation.AssemblyName}' to' {reference.Display}' could not be resolved." );

                    compileTimeProject = null;

                    return false;
                }
            }

            if ( !this._builder.TryGetCompileTimeProject(
                    compilationContext,
                    compileTimeTreesHint,
                    referencedProjects,
                    diagnosticSink,
                    cacheOnly,
                    out compileTimeProject,
                    cancellationToken ) )
            {
                this._logger.Warning?.Log( $"TryGetCompileTimeProjectFromCompilation('{compilationContext.SourceCompilation.AssemblyName}'): failed." );

                compileTimeProject = null;

                return false;
            }

            compileTimeProject = this.RegisterProject( runTimeCompilation.Assembly.Identity, compileTimeProject );

            this._logger.Trace?.Log( $"TryGetCompileTimeProjectFromCompilation('{compilationContext.SourceCompilation.AssemblyName}'): successful." );

            return true;
        }

        private bool TryGetCompileTimeProject(
            MetadataReference reference,
            IDiagnosticAdder diagnosticSink,
            bool cacheOnly,
            CancellationToken cancellationToken,
            out CompileTimeProject? referencedProject )
        {
            switch ( reference )
            {
                case PortableExecutableReference { FilePath: { } filePath }:
                    return this.TryGetCompileTimeProjectFromPath(
                        filePath,
                        diagnosticSink,
                        cacheOnly,
                        cancellationToken,
                        out referencedProject );

                case CompilationReference compilationReference:
                    // The same assembly identity can be reached through more than one reference, so serve a project
                    // this Builder has already resolved from the cache, as the PortableExecutableReference path does.
                    // This also keeps the Add below from throwing on a duplicate key.
                    if ( this._projects.TryGetValue( compilationReference.Compilation.Assembly.Identity, out referencedProject ) )
                    {
                        return true;
                    }

                    // Issue #1611: at design time, when the upstream's pipeline is already running and has a built
                    // CompileTimeProject, reuse it rather than recursively building a fresh projection. This ensures
                    // both pipelines share the same physical loaded assembly for the upstream and prevents the
                    // welding-site mismatch where IAspectClass.Type and the live IAspect come from two different
                    // physical projections of the same logical upstream.
                    if ( this._serviceProvider.Global.GetService<IUpstreamCompileTimeProjectProvider>() is { } upstreamProvider
                         && upstreamProvider.TryGetUpstreamConfiguration( compilationReference.Compilation, out var upstreamConfig )
                         && upstreamConfig.CompileTimeProject is { } upstreamProject )
                    {
                        // Cache by AssemblyIdentity so subsequent identity-keyed lookups within this Builder hit the same instance.
                        referencedProject = this.RegisterProject( upstreamProject.RunTimeIdentity, upstreamProject );

                        this._logger.Trace?.Log(
                            $"Reusing upstream pipeline's CompileTimeProject for '{compilationReference.Compilation.AssemblyName}' (issue #1611)." );

                        return true;
                    }

                    return this.TryGetCompileTimeProjectFromCompilation(
                        compilationReference.Compilation,
                        null,
                        diagnosticSink,
                        cacheOnly,
                        cancellationToken,
                        out referencedProject );

                default:
                    throw new AssertionFailedException( $"Unexpected reference kind: {reference}." );
            }
        }

        private bool TryGetCompileTimeProjectFromPath(
            string assemblyPath,
            IDiagnosticAdder diagnosticSink,
            bool cacheOnly,
            CancellationToken cancellationToken,
            out CompileTimeProject? compileTimeProject )
        {
            if ( !File.Exists( assemblyPath ) )
            {
                this._logger.Warning?.Log( $"The file '{assemblyPath}' does not exist." );

                compileTimeProject = null;

                return false;
            }

            var assemblyIdentity = MetadataReferenceCache.GetAssemblyName( assemblyPath ).ToAssemblyIdentity();

            // If the assembly is a standard one, there is no need to analyze.
            if ( this._serviceProvider.GetReferenceAssemblyLocator().AssemblyNames.Contains( assemblyIdentity.Name ) )
            {
                compileTimeProject = null;

                this._logger.Trace?.Log( $"'{assemblyPath}' is a standard assembly." );

                return true;
            }

            // Look in our cache.
            if ( this._projects.TryGetValue( assemblyIdentity, out compileTimeProject ) )
            {
                this._logger.Trace?.Log( $"'{assemblyPath}' was found in cache." );

                return true;
            }

            // LoadFromAssemblyPath throws for mscorlib
            if ( Path.GetFileNameWithoutExtension( assemblyPath ) == typeof(object).Assembly.GetName().Name )
            {
                this._logger.Trace?.Log( $"'{assemblyPath}' is a system assembly." );

                goto finish;
            }

            // Performance trick: do not analyze system assemblies.
            var assemblyFileName = Path.GetFileNameWithoutExtension( assemblyPath );

            if ( assemblyFileName.Equals( "System", StringComparison.OrdinalIgnoreCase ) ||
                 assemblyFileName.StartsWith( "System.", StringComparison.OrdinalIgnoreCase ) ||
                 assemblyFileName.StartsWith( "Microsoft.CodeAnalysis", StringComparison.OrdinalIgnoreCase ) )
            {
                this._logger.Trace?.Log( $"'{assemblyPath}' is a system assembly." );

                goto finish;
            }

            if ( !MetadataReader.TryGetMetadata( assemblyPath, out var metadataInfo ) )
            {
                this._logger.Warning?.Log( $"Could not read metadata from '{assemblyPath}'." );

                goto finish;
            }

            if ( metadataInfo.Resources.TryGetValue( CompileTimeConstants.CompileTimeProjectResourceName, out var resourceBytes ) )
            {
                this._cacheableTemplateDiscoveryContextProvider.OnPortableExecutableReferenceDiscovered();

                var assemblyName = MetadataReferenceCache.GetAssemblyName( assemblyPath );

                if ( !this.TryDeserializeCompileTimeProject(
                        assemblyName.ToAssemblyIdentity(),
                        new MemoryStream( resourceBytes ),
                        diagnosticSink,
                        cacheOnly,
                        this._cacheableTemplateDiscoveryContextProvider,
                        out compileTimeProject,
                        cancellationToken ) )
                {
                    this._logger.Warning?.Log( $"TryDeserializeCompileTimeProject('{assemblyPath}') failed." );

                    // Coverage: ignore

                    return false;
                }
            }
            else if ( metadataInfo.HasCompileTimeAttribute )
            {
                // We have an assembly that a [assembly: CompileTime] attribute but has no embedded compile-time project.
                // This is typically the case of public assemblies of weaver-based aspects or services.
                // These projects need to be included as compile-time projects. They typically have MetalamaRemoveCompileTimeOnlyCode=false.
                if ( !CompileTimeProject.TryCreateUntransformed(
                        this._serviceProvider,
                        this._domain,
                        assemblyIdentity,
                        assemblyPath,
                        this._cacheableTemplateDiscoveryContextProvider,
                        out compileTimeProject ) )
                {
                    this._logger.Warning?.Log(
                        $"The assembly '{assemblyPath}' will not be included in the compile-time compilation despite having an [assembly: CompileTime] attribute "
                        +
                        "because it has no compile-time embedded resource and it is not loaded as an analyzer." );
                }
            }
            else
            {
                this._logger.Trace?.Log( $"'{assemblyPath}' does not contain compile-time code." );
            }

        finish:
            compileTimeProject = this.RegisterProject( assemblyIdentity, compileTimeProject );

            return true;
        }

        private bool TryDeserializeCompileTimeProject(
            AssemblyIdentity runTimeAssemblyIdentity,
            Stream resourceStream,
            IDiagnosticAdder diagnosticAdder,
            bool cacheOnly,
            CacheableTemplateDiscoveryContextProvider? cacheableTemplateDiscoveryContextProvider,
            [NotNullWhen( true )] out CompileTimeProject? project,
            CancellationToken cancellationToken )
        {
            using var archive = new ZipArchive( resourceStream, ZipArchiveMode.Read, true, Encoding.UTF8 );

            // Read manifest.
            var manifestEntry = archive.GetEntry( "manifest.json" ).AssertNotNull();

            var manifest = CompileTimeProjectManifest.Deserialize( manifestEntry.Open() );

            // Check the manifest version.
            if ( manifest.ManifestVersion != CompileTimeProjectManifest.CurrentManifestVersion )
            {
                diagnosticAdder.Report(
                    GeneralDiagnosticDescriptors.DependencyMustBeRecompiled.CreateRoslynDiagnostic(
                        null,
                        (runTimeAssemblyIdentity, manifest.MetalamaVersion) ) );

                project = null;

                return false;
            }

            // Read source files.
            var parseOptions = new CSharpParseOptions( manifest.LanguageVersion ?? SupportedCSharpVersions.Latest );

            List<SyntaxTree> syntaxTrees = [];

            foreach ( var entry in archive.Entries.Where( e => string.Equals( Path.GetExtension( e.Name ), ".cs", StringComparison.OrdinalIgnoreCase ) ) )
            {
                using var sourceReader = new StreamReader( entry.Open(), Encoding.UTF8 );
                var sourceText = sourceReader.ReadToEnd();
                var syntaxTree = CSharpSyntaxTree.ParseText( sourceText, parseOptions ).WithFilePath( entry.FullName );
                syntaxTrees.Add( syntaxTree );
            }

            // Resolve references.
            List<CompileTimeProject> referenceProjects = [];

            if ( manifest.References != null )
            {
                foreach ( var referenceSerializedIdentity in manifest.References )
                {
                    var referenceAssemblyIdentity = new AssemblyName( referenceSerializedIdentity ).ToAssemblyIdentity();

                    if ( !this.TryGetCompileTimeProject(
                            referenceAssemblyIdentity,
                            diagnosticAdder,
                            cacheOnly,
                            cancellationToken,
                            out var referenceProject ) )
                    {
                        // Coverage: ignore
                        // (this happens when the project reference could not be resolved.)

                        project = null;

                        this._logger.Warning?.Log(
                            $"TryDeserializeCompileTimeProject('{runTimeAssemblyIdentity}'): processing of reference '{referenceAssemblyIdentity}' failed." );

                        return false;
                    }

                    if ( referenceProject != null )
                    {
                        referenceProjects.Add( referenceProject );
                    }
                }
            }

            // Deserialize the project.
            if ( !this._builder.TryCompileDeserializedProject(
                    runTimeAssemblyIdentity,
                    manifest,
                    syntaxTrees,
                    referenceProjects,
                    diagnosticAdder,
                    cancellationToken,
                    out var compileTimeAssemblyName,
                    out var assemblyPath,
                    out var sourceDirectory ) )
            {
                // Coverage: ignore
                // (this happens when the compile-time could not be compiled into a binary assembly.)

                this._logger.Warning?.Log( $"TryDeserializeCompileTimeProject('{runTimeAssemblyIdentity}'): TryCompileDeserializedProject failed'." );

                project = null;

                return false;
            }

            // Compute the new hash.

            project = CompileTimeProject.Create(
                this._serviceProvider,
                this._domain,
                runTimeAssemblyIdentity,
                new AssemblyIdentity( compileTimeAssemblyName ),
                referenceProjects,
                manifest,
                assemblyPath,
                sourceDirectory,
                FullPathTextMapFileProvider.Instance,
                cacheableTemplateDiscoveryContextProvider );

            return true;
        }

        /// <summary>
        /// Tries to get the <see cref="CompileTimeProject"/> given its <see cref="AssemblyIdentity"/>.
        /// </summary>
        private bool TryGetCompileTimeProject(
            AssemblyIdentity runTimeAssemblyIdentity,
            IDiagnosticAdder diagnosticAdder,
            bool cacheOnly,
            CancellationToken cancellationToken,
            out CompileTimeProject? compileTimeProject )
        {
            if ( this._projects.TryGetValue( runTimeAssemblyIdentity, out compileTimeProject ) )
            {
                return true;
            }
            else
            {
                if ( this._runTimeAssemblyLocator.TryFindAssembly( runTimeAssemblyIdentity, out var metadataReference ) != true )
                {
                    var diagnostic = GeneralDiagnosticDescriptors.CannotFindCompileTimeAssembly.CreateRoslynDiagnostic(
                        Location.None,
                        runTimeAssemblyIdentity );

                    diagnosticAdder.Report( diagnostic );
                    this._logger.Warning?.Log( diagnostic.ToString() );

                    compileTimeProject = null;

                    return false;
                }

                return this.TryGetCompileTimeProject(
                    metadataReference,
                    diagnosticAdder,
                    cacheOnly,
                    cancellationToken,
                    out compileTimeProject );
            }
        }
    }
}