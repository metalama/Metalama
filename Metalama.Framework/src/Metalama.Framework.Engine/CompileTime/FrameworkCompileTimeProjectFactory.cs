// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class FrameworkCompileTimeProjectFactory : IGlobalService
{
    private static readonly Assembly _frameworkAssembly = typeof(IAspect).Assembly;
    private static readonly AssemblyIdentity _frameworkAssemblyIdentity = _frameworkAssembly.GetName().ToAssemblyIdentity();

    /// <summary>
    /// The manifest of the framework compile-time project, indexed by the path of the metadata reference it was built
    /// from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The path identifies the assembly exactly, which is what the manifest depends on: the package ships one assembly
    /// per target framework, and the process analyzes projects that can reference different versions of the package,
    /// because <c>AspectPipeline.TryInitialize</c> rejects only a version newer than the engine.
    /// </para>
    /// <para>
    /// The path was previously the target framework moniker read from the <see cref="TargetFrameworkAttribute"/> of the
    /// referenced assembly. That value neither identifies the version nor is always readable, and the read was an
    /// assertion, so a compilation that could not supply it aborted the initialization of the pipeline. See issue #1820.
    /// </para>
    /// <para>
    /// Compared with <see cref="StringComparer.Ordinal"/> rather than case-insensitively, because two spellings of one
    /// path only cost a second entry, while two paths that differ in case are two files on a case-sensitive file system.
    /// </para>
    /// <para>
    /// The entry records the time the file was last written, so that an assembly rebuilt at the same path is not served
    /// from an entry describing the previous build. This is the invalidation that <see cref="MetadataReader"/> applies
    /// to its own cache of the same files.
    /// </para>
    /// </remarks>
    private readonly ConcurrentDictionary<string, CachedManifest> _manifestsByReferencePath = new( StringComparer.Ordinal );

    /// <summary>
    /// A manifest together with the time the file it was built from was last written, or <c>null</c> when the path does
    /// not name a file on disk.
    /// </summary>
    private readonly record struct CachedManifest( DateTime? LastFileWrite, CompileTimeProjectManifest Manifest );

    /// <summary>
    /// Returns the time the file at the given path was last written, or <c>null</c> when the path is the display string
    /// of an embedded assembly rather than a path on disk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The path of a metadata reference is not necessarily a file. A reference created from a stream carries a display
    /// string instead, which is what <see cref="CompileTimeAssemblyLocator"/> produces for the assemblies it embeds as
    /// manifest resources. The entry of such a reference is never invalidated, which is correct: its content is inside
    /// the engine assembly and cannot change while the process runs.
    /// </para>
    /// <para>
    /// Only that one form is recognized, by its shape and not by asking the file system, because it is the only one the
    /// engine produces. Any other path is given to <see cref="File.GetLastWriteTime(string)"/>, which answers with a constant
    /// for a path that names no existing file, so an assembly that appears at that path later is not served from an
    /// entry built before it existed.
    /// </para>
    /// </remarks>
    private static DateTime? GetLastFileWriteOrNull( string path )
        => CompileTimeAssemblyLocator.IsEmbeddedAssemblyFilePath( path ) ? null : File.GetLastWriteTime( path );

    private static DiagnosticManifest CreateFrameworkDiagnosticManifest()
    {
        var additionalTypes = new[] { typeof(FrameworkDiagnosticDescriptors) };
        var service = new DiagnosticDefinitionDiscoveryService();
        var diagnostics = service.GetDiagnosticDefinitions( additionalTypes ).ToImmutableArray();
        var suppressions = service.GetSuppressionDefinitions( additionalTypes ).ToImmutableArray();

        return new DiagnosticManifest( diagnostics, suppressions );
    }

    /// <summary>
    /// Builds the manifest that indexes the template members of the given <c>Metalama.Framework</c> assembly.
    /// </summary>
    /// <remarks>
    /// A type that the assembly does not expose is reported and skipped rather than asserted. The assembly is a
    /// reference of a compilation that the engine does not control, so it can be one that no type can be read from: a
    /// stale or truncated file, or a reference of a compilation whose references are incomplete. The pipeline then
    /// initializes with a manifest that indexes fewer templates, which degrades the analysis of the affected project
    /// instead of aborting it. See issue #1820.
    /// </remarks>
    private static TemplateProjectManifest CreateFrameworkTemplateProjectManifest( IAssemblySymbol assembly, ILogger logger )
    {
        // Create a builder.
        var builder = new TemplateProjectManifestBuilder( assembly.GlobalNamespace );

        // Index all template members.
        var typesDefiningTemplates = new[] { typeof(OverrideFieldOrPropertyAspect), typeof(OverrideMethodAspect), typeof(OverrideEventAspect) };

        foreach ( var reflectionType in typesDefiningTemplates )
        {
            var typeSymbol = assembly.GetTypeByMetadataName( reflectionType.FullName! );

            if ( typeSymbol == null )
            {
                logger.Error?.Log(
                    $"The type '{reflectionType.FullName}' cannot be read from assembly '{assembly.Identity}'. Its templates are "
                    + "not indexed in the manifest of the framework compile-time project." );

                continue;
            }

            foreach ( var member in typeSymbol.GetMembers() )
            {
                if ( member.GetAttributes().Any( a => a.AttributeClass?.Name == nameof(TemplateAttribute) ) )
                {
                    var templateInfo = new TemplateInfo( TemplateAttributeType.Template, true );
                    builder.AddOrUpdateSymbol( member, TemplatingScope.CompileTimeOnly, templateInfo );

                    // Also add to accessors.
                    void AddAccessor( IMethodSymbol? accessor )
                    {
                        if ( accessor != null )
                        {
                            builder.AddOrUpdateSymbol( accessor, TemplatingScope.CompileTimeOnly, templateInfo );

                            // Mark parameters as run-time.
                            foreach ( var parameter in accessor.Parameters )
                            {
                                builder.AddOrUpdateSymbol( parameter, TemplatingScope.RunTimeOnly );
                            }
                        }
                    }

                    switch ( member.Kind )
                    {
                        case SymbolKind.Method when member is IMethodSymbol method:
                            // Mark parameters as run-time.
                            foreach ( var parameter in method.Parameters )
                            {
                                builder.AddOrUpdateSymbol( parameter, TemplatingScope.RunTimeOnly );
                            }

                            break;

                        case SymbolKind.Property when member is IPropertySymbol property:
                            AddAccessor( property.GetMethod );
                            AddAccessor( property.SetMethod );

                            break;

                        case SymbolKind.Event when member is IEventSymbol @event:
                            AddAccessor( @event.AddMethod );
                            AddAccessor( @event.RemoveMethod );

                            break;
                    }
                }
            }
        }

        return builder.Build();
    }

    private static CompileTimeProjectManifest CreateFrameworkProjectManifest( IAssemblySymbol assembly, ILogger logger )
        => new(
            _frameworkAssemblyIdentity.ToString(),
            "",
            ImmutableArray<string>.Empty,
            ImmutableArray<string>.Empty,
            ImmutableArray<string>.Empty,
            ImmutableArray<string>.Empty,
            ImmutableArray<string>.Empty,
            ImmutableArray<string>.Empty,
            ImmutableArray<string>.Empty,
            CreateFrameworkTemplateProjectManifest( assembly, logger ),
            0,
            Array.Empty<CompileTimeFileManifest>(),
            Array.Empty<CompileTimeDiagnosticManifest>(),
            false );

    public CompileTimeProject CreateFrameworkProject( in ProjectServiceProvider serviceProvider, CompileTimeDomain domain, Compilation compilation )
    {
        var assembly = compilation.SourceModule.ReferencedAssemblySymbols.First( x => x.Name == "Metalama.Framework" );
        var logger = serviceProvider.GetLoggerFactory().CompileTime();

        CompileTimeProjectManifest manifest;

        if ( compilation.GetMetadataReference( assembly ) is PortableExecutableReference { FilePath: { } path } )
        {
            var lastFileWrite = GetLastFileWriteOrNull( path );

            if ( !this._manifestsByReferencePath.TryGetValue( path, out var cached ) || cached.LastFileWrite != lastFileWrite )
            {
                cached = new CachedManifest( lastFileWrite, CreateFrameworkProjectManifest( assembly, logger ) );
                this._manifestsByReferencePath[path] = cached;
            }

            manifest = cached.Manifest;
        }
        else
        {
            // A CompilationReference, which is the form a project reference takes at design time, carries no path, and
            // neither does a reference created from an image. Nothing then identifies the assembly across compilations,
            // so the manifest is built for this call only. Reported as an error because the compilations the pipeline is
            // given are expected to reference Metalama.Framework as a file, so this indicates an arrangement that has
            // not been accounted for.
            logger.Error?.Log(
                $"The reference to assembly '{assembly.Identity}' carries no path. The manifest of the framework compile-time "
                + "project is built for this compilation instead of being shared." );

            manifest = CreateFrameworkProjectManifest( assembly, logger );
        }

        return new CompileTimeProject(
            serviceProvider,
            domain,
            _frameworkAssemblyIdentity,
            _frameworkAssemblyIdentity,
            ImmutableArray<CompileTimeProject>.Empty,
            manifest,
            null,
            null,
            null,
            null,
            _frameworkAssembly,
            CreateFrameworkDiagnosticManifest() );
    }
}