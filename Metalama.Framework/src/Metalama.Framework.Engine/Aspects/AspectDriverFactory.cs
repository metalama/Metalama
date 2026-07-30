// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// Creates instances of <see cref="IAspectDriver"/> for a given <see cref="AspectClass"/>.
/// </summary>
internal sealed class AspectDriverFactory
{
    private readonly CompilationModel _compilation;
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly ImmutableDictionary<string, IAspectDriver> _weaverTypes;

    public AspectDriverFactory(
        CompilationModel compilation,
        ImmutableArray<object> plugins,
        in ProjectServiceProvider serviceProvider,
        IDiagnosticAdder diagnosticAdder )
    {
        this._compilation = compilation;
        this._serviceProvider = serviceProvider;

        var weaverTypesBuilder = ImmutableDictionary.CreateBuilder<string, IAspectDriver>();

        // The plug-ins are ordered by the order of the references, which is not stable, so we sort them by
        // assembly-qualified type name to make the weaver kept for a duplicated type name deterministic.
        foreach ( var weaver in plugins.OfType<IAspectDriver>().OrderBy( d => d.GetType().AssemblyQualifiedName, StringComparer.Ordinal ) )
        {
            var weaverType = weaver.GetType();
            var weaverTypeName = weaverType.FullName.AssertNotNull();

            if ( !weaverTypesBuilder.TryGetValue( weaverTypeName, out var existingWeaver ) )
            {
                weaverTypesBuilder.Add( weaverTypeName, weaver );

                continue;
            }

            // The same weaver type name was contributed twice, which happens when the same aspect library reaches the
            // compilation through two routes, for instance as a package and through a project reference (issue #1743).
            // We keep the first weaver in both branches below, because the dictionary is keyed by the name that
            // RequireAspectWeaverAttribute stores, so there is nothing left to tell the two entries apart afterwards.
            var existingAssembly = existingWeaver.GetType().Assembly.FullName;
            var duplicateAssembly = weaverType.Assembly.FullName;

            if ( string.Equals( existingAssembly, duplicateAssembly, StringComparison.Ordinal ) )
            {
                // The two weavers come from the same assembly identity, so they are truly duplicate: plug-ins are
                // instantiated with their default constructor, so the instances are interchangeable and the
                // duplication is not worth a user-visible diagnostic.
                this._serviceProvider.GetLoggerFactory()
                    .GetLogger( nameof(AspectDriverFactory) )
                    .Warning?.Log( $"The aspect weaver '{weaverTypeName}' was provided twice by '{existingAssembly}'. Keeping the first one." );
            }
            else
            {
                // The two weavers come from assemblies of a different identity, so the instances are not
                // interchangeable and we cannot know which one the user means.
                diagnosticAdder.Report(
                    GeneralDiagnosticDescriptors.DuplicateAspectWeaver.CreateRoslynDiagnostic(
                        Location.None,
                        (weaverTypeName, existingAssembly ?? existingWeaver.GetType().Name, duplicateAssembly ?? weaverType.Name) ) );
            }
        }

        this._weaverTypes = weaverTypesBuilder.ToImmutable();
    }

    public IAspectDriver GetAspectDriver( AspectClass aspectClass )
    {
        if ( aspectClass.WeaverType != null )
        {
            if ( !this._weaverTypes.TryGetValue( aspectClass.WeaverType, out var registeredAspectDriver ) )
            {
                // It's okay to have a missing driver if the aspect is not instantiated.
                // This is actually a common situation when building the project defining the aspect class.
                // Return an ErrorAspectWeaver that will emit an error when used.
                return new ErrorAspectWeaver( aspectClass );
            }

            return registeredAspectDriver;
        }

        return new AspectDriver( this._serviceProvider, aspectClass, this._compilation );
    }
}