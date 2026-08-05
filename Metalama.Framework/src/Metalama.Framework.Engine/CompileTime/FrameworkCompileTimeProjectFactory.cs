// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
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
    /// </remarks>
    private readonly ConcurrentDictionary<string, CompileTimeProjectManifest> _manifestsByReferencePath = new( StringComparer.Ordinal );

    private static DiagnosticManifest CreateFrameworkDiagnosticManifest()
    {
        var additionalTypes = new[] { typeof(FrameworkDiagnosticDescriptors) };
        var service = new DiagnosticDefinitionDiscoveryService();
        var diagnostics = service.GetDiagnosticDefinitions( additionalTypes ).ToImmutableArray();
        var suppressions = service.GetSuppressionDefinitions( additionalTypes ).ToImmutableArray();

        return new DiagnosticManifest( diagnostics, suppressions );
    }

    private static TemplateProjectManifest CreateFrameworkTemplateProjectManifest( IAssemblySymbol assembly )
    {
        // Create a builder.
        var builder = new TemplateProjectManifestBuilder( assembly.GlobalNamespace );

        // Index all template members.
        var typesDefiningTemplates = new[] { typeof(OverrideFieldOrPropertyAspect), typeof(OverrideMethodAspect), typeof(OverrideEventAspect) };

        foreach ( var reflectionType in typesDefiningTemplates )
        {
            var typeSymbol = assembly.GetTypeByMetadataName( reflectionType.FullName! ).AssertNotNull();

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

    private static CompileTimeProjectManifest CreateFrameworkProjectManifest( IAssemblySymbol assembly )
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
            CreateFrameworkTemplateProjectManifest( assembly ),
            0,
            Array.Empty<CompileTimeFileManifest>(),
            Array.Empty<CompileTimeDiagnosticManifest>(),
            false );

    public CompileTimeProject CreateFrameworkProject( in ProjectServiceProvider serviceProvider, CompileTimeDomain domain, Compilation compilation )
    {
        var assembly = compilation.SourceModule.ReferencedAssemblySymbols.First( x => x.Name == "Metalama.Framework" );

        CompileTimeProjectManifest manifest;

        if ( compilation.GetMetadataReference( assembly ) is PortableExecutableReference { FilePath: { } path } )
        {
            manifest = this._manifestsByReferencePath.GetOrAdd(
                path,
                static ( _, a ) => CreateFrameworkProjectManifest( a ),
                assembly );
        }
        else
        {
            // The reference has no path when it is a CompilationReference, that is, when the analyzed project references
            // Metalama.Framework as a project of the same solution, or when it was created from an image. Nothing then
            // identifies the assembly across compilations, so the manifest is built for this call only, which is correct
            // at the cost of not being shared.
            serviceProvider.GetLoggerFactory()
                .CompileTime()
                .Trace?.Log(
                    $"The reference to assembly '{assembly.Identity}' has no path. The manifest of the framework compile-time "
                    + "project is built for this compilation instead of being shared." );

            manifest = CreateFrameworkProjectManifest( assembly );
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