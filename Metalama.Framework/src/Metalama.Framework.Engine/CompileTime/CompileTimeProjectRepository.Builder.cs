// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating.Mapping;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly IProjectOptions? _projectOptions;

        // The run-time identity that claimed each compile-time assembly name, used to detect two references providing
        // the same compile-time assembly. Both kinds of project can collide, so both reserve their name. An
        // untransformed project keeps the run-time name of the assembly it is built from, so two versions of it collide
        // directly. A transformed one takes an 'ml!<name>_<hash>' name whose hash covers the assembly name and version
        // but neither the culture nor the public key, which are precisely the components that let two assemblies of one
        // simple name be referenced side by side. See #1749.
        private readonly Dictionary<string, AssemblyIdentity> _runTimeIdentityByCompileTimeName = new( StringComparer.OrdinalIgnoreCase );

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
            this._projectOptions = serviceProvider.GetService<IProjectOptions>();
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

            loader = new CompileTimeProjectRepository(
                this._domain,
                this._serviceProvider,
                this._projects,
                compileTimeProject );

            return true;
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

            this._projects.Add( runTimeCompilation.Assembly.Identity, compileTimeProject );

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
                        // The provider resolves the upstream pipeline by ProjectKey, which is an assembly name and a
                        // hash of the preprocessor symbols and carries no version, so two projects that produce one
                        // assembly name at two versions share a single pipeline slot. The project handed back is then
                        // the wrong one: reusing it would give this reference another version's compile-time project,
                        // and caching it under its own identity would collide with the entry the other reference
                        // already made, which threw here (issue #1749). Reuse only what actually matches, and let a
                        // mismatch fall through to the recursive build below, which is correct if slower.
                        if ( upstreamProject.RunTimeIdentity.Equals( compilationReference.Compilation.Assembly.Identity ) )
                        {
                            // Cache by AssemblyIdentity so subsequent identity-keyed lookups within this Builder hit the same instance.
                            this._projects.Add( upstreamProject.RunTimeIdentity, upstreamProject );
                            referencedProject = upstreamProject;

                            this._logger.Trace?.Log(
                                $"Reusing upstream pipeline's CompileTimeProject for '{compilationReference.Compilation.AssemblyName}' (issue #1611)." );

                            return true;
                        }

                        this._logger.Warning?.Log(
                            $"The upstream pipeline of '{compilationReference.Compilation.AssemblyName}' provides a compile-time project for "
                            + $"'{upstreamProject.RunTimeIdentity}', which is not the identity of the reference "
                            + $"('{compilationReference.Compilation.Assembly.Identity}'). Building the compile-time project instead of reusing it." );
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

                // A transformed project takes an 'ml!<name>_<hash>' compile-time name, and the hash was long assumed to
                // make that name unique per content. It does not: it covers the assembly name and version but neither
                // the culture nor the public key, which are exactly the two components the C# compiler lets differ so
                // that two assemblies of one simple name can be used side by side. Two such references therefore claim
                // one compile-time name, which used to surface as an unhandled ArgumentException from
                // CompileTimeProject.ClosureProjectsByCompileTimeAssemblyName (issue #1749).
                if ( !this.TryReserveCompileTimeAssemblyName( compileTimeProject.CompileTimeIdentity.Name, assemblyIdentity, diagnosticSink ) )
                {
                    compileTimeProject = null;

                    return false;
                }
            }
            else if ( metadataInfo.HasCompileTimeAttribute )
            {
                // We have an assembly that a [assembly: CompileTime] attribute but has no embedded compile-time project.
                // This is typically the case of public assemblies of weaver-based aspects or services.
                // These projects need to be included as compile-time projects. They typically have MetalamaRemoveCompileTimeOnlyCode=false.

                // Such a project keeps the run-time name of its assembly as its compile-time name, so two versions of
                // it claim the same compile-time assembly. Detected here rather than left to the assembly load, which
                // fails with an unhandled FileLoadException because a single AssemblyLoadContext cannot hold two
                // assemblies of one simple name (issue #1749).
                if ( !this.TryReserveCompileTimeAssemblyName( assemblyIdentity.Name, assemblyIdentity, diagnosticSink ) )
                {
                    compileTimeProject = null;

                    return false;
                }

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
            this._projects.Add( assemblyIdentity, compileTimeProject );

            return true;
        }

        /// <summary>
        /// Records that a run-time assembly claims a compile-time assembly name, and reports an error when another
        /// assembly has already claimed the same name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Returns <c>true</c> when the name was free, or when the same assembly claims it again, which happens when
        /// one reference is reached through several paths.
        /// </para>
        /// <para>
        /// The diagnostic names the <em>run-time</em> assembly and not the compile-time one, because that is what the
        /// user can act on. The two are the same for an untransformed project, but a transformed one is named
        /// <c>ml!&lt;name&gt;_&lt;hash&gt;</c>, which means nothing outside Metalama. Nothing is lost by reporting the
        /// run-time name: the compile-time name embeds the run-time name and its hash covers it, so two assemblies can
        /// claim one compile-time name only if they share a run-time name too.
        /// </para>
        /// </remarks>
        private bool TryReserveCompileTimeAssemblyName(
            string compileTimeAssemblyName,
            AssemblyIdentity runTimeIdentity,
            IDiagnosticAdder diagnosticSink )
        {
            if ( !this._runTimeIdentityByCompileTimeName.TryGetValue( compileTimeAssemblyName, out var claimant ) )
            {
                this._runTimeIdentityByCompileTimeName.Add( compileTimeAssemblyName, runTimeIdentity );

                return true;
            }

            if ( claimant.Equals( runTimeIdentity ) )
            {
                return true;
            }

            this._logger.Error?.Log(
                $"Both '{claimant}' and '{runTimeIdentity}' provide the compile-time assembly '{compileTimeAssemblyName}'." );

            diagnosticSink.Report(
                GeneralDiagnosticDescriptors.DuplicateCompileTimeAssemblyName.CreateRoslynDiagnostic(
                    null,
                    (runTimeIdentity.Name, claimant, runTimeIdentity) ) );

            return false;
        }

        /// <summary>
        /// Warns when a reference that comes from a <c>ProjectReference</c> was compiled by another version of
        /// Metalama than the current one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Restricted to project references on purpose. Consuming a package built with another version of Metalama is
        /// normal and works, at build time and in the IDE alike, because a package is consumed statically, through the
        /// compile-time project embedded in it. Two projects of the same solution on two versions is a different
        /// matter: it is a configuration the user controls, it is slower, and above a generation boundary the IDE
        /// cannot consume the reference at all.
        /// </para>
        /// <para>
        /// The reference is matched by assembly name and not by path. Paths do not work: the compiler is given the
        /// reference assembly under <c>obj</c>, whereas MSBuild's item holds the implementation assembly under
        /// <c>bin</c>, so a path comparison fails for every project reference and reports nothing. A name is also
        /// immune to normalization, casing and link resolution, and cannot contain the separator of the list.
        /// </para>
        /// <para>
        /// When <see cref="IProjectOptions.ProjectReferenceNames"/> is empty, which is the case for a host that does
        /// not supply it, nothing is reported rather than everything.
        /// </para>
        /// </remarks>
        private void ReportMixedVersionWarnings(
            AssemblyIdentity runTimeAssemblyIdentity,
            CompileTimeProjectManifest manifest,
            IDiagnosticAdder diagnosticAdder )
        {
            var projectReferenceNames = this._projectOptions?.ProjectReferenceNames ?? ImmutableArray<string>.Empty;

            if ( projectReferenceNames.IsDefaultOrEmpty
                 || !projectReferenceNames.Any( n => string.Equals( n, runTimeAssemblyIdentity.Name, StringComparison.OrdinalIgnoreCase ) ) )
            {
                return;
            }

            var currentVersion = AssemblyMetadataReader.GetInstance( typeof(CompileTimeProjectManifest).Assembly ).PackageVersion;

            if ( string.Equals( manifest.MetalamaVersion, currentVersion, StringComparison.OrdinalIgnoreCase ) )
            {
                return;
            }

            // The generation boundary is the more specific and more actionable of the two, so it replaces the general
            // warning rather than adding to it.
            if ( !DesignTimeCompatibility.IsSupportedAtDesignTime( manifest.MetalamaVersion ) )
            {
                diagnosticAdder.Report(
                    GeneralDiagnosticDescriptors.ReferenceNotSupportedAtDesignTime.CreateRoslynDiagnostic(
                        null,
                        (runTimeAssemblyIdentity, manifest.MetalamaVersion, DesignTimeCompatibility.MinimumSupportedVersion.ToString()) ) );
            }
            else
            {
                diagnosticAdder.Report(
                    GeneralDiagnosticDescriptors.MixedMetalamaVersionsInSolution.CreateRoslynDiagnostic(
                        null,
                        (runTimeAssemblyIdentity, manifest.MetalamaVersion, currentVersion ?? "") ) );
            }
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

            this.ReportMixedVersionWarnings( runTimeAssemblyIdentity, manifest, diagnosticAdder );

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