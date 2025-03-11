// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
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

    private readonly ConcurrentDictionary<string, CompileTimeProjectManifest> _frameworkProjectManifestDictionary = new();

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

                    switch ( member )
                    {
                        case IMethodSymbol method:
                            // Mark parameters as run-time.
                            foreach ( var parameter in method.Parameters )
                            {
                                builder.AddOrUpdateSymbol( parameter, TemplatingScope.RunTimeOnly );
                            }

                            break;

                        case IPropertySymbol property:
                            AddAccessor( property.GetMethod );
                            AddAccessor( property.SetMethod );

                            break;

                        case IEventSymbol @event:
                            AddAccessor( @event.AddMethod );
                            AddAccessor( @event.RemoveMethod );

                            break;
                    }
                }
            }
        }

        return builder.Build();
    }

    public CompileTimeProject CreateFrameworkProject( in ProjectServiceProvider serviceProvider, CompileTimeDomain domain, Compilation compilation )
    {
        var assembly = compilation.SourceModule.ReferencedAssemblySymbols.First( x => x.Name == "Metalama.Framework" );

        if ( assembly.GetAttributes()
                .SingleOrDefault( attribute => attribute.AttributeClass?.Name == nameof(TargetFrameworkAttribute) )
                ?.ConstructorArguments
                .SingleOrDefault()
                .Value is not string tfm )
        {
            throw new AssertionFailedException( $"Cannot read the TargetFrameworkAttribute from assembly '{assembly.Identity}'." );
        }

        var manifest = this._frameworkProjectManifestDictionary.GetOrAdd(
            tfm,
            static ( _, a ) => new CompileTimeProjectManifest(
                _frameworkAssemblyIdentity.ToString(),
                "",
                ImmutableArray<string>.Empty,
                ImmutableArray<string>.Empty,
                ImmutableArray<string>.Empty,
                ImmutableArray<string>.Empty,
                ImmutableArray<string>.Empty,
                ImmutableArray<string>.Empty,
                ImmutableArray<string>.Empty,
                CreateFrameworkTemplateProjectManifest( a ),
                0,
                Array.Empty<CompileTimeFileManifest>(),
                Array.Empty<CompileTimeDiagnosticManifest>(),
                false ),
            assembly );

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