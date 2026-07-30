// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
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

    public AspectDriverFactory( CompilationModel compilation, ImmutableArray<object> plugins, in ProjectServiceProvider serviceProvider )
    {
        this._compilation = compilation;
        this._serviceProvider = serviceProvider;

        var weaverTypesBuilder = ImmutableDictionary.CreateBuilder<string, IAspectDriver>();

        // The plug-ins are ordered by the order of the references, which is not stable, so we sort them by
        // assembly-qualified type name to make the weaver selected below deterministic.
        foreach ( var weaver in plugins.OfType<IAspectDriver>().OrderBy( d => d.GetType().AssemblyQualifiedName, StringComparer.Ordinal ) )
        {
            var weaverTypeName = weaver.GetType().FullName.AssertNotNull();

            // The same weaver type name can be contributed twice when the same aspect library reaches the compilation
            // through two routes, for instance as a package and through a project reference (issue #1743). Plug-ins are
            // instantiated with their default constructor, so the duplicates are interchangeable and we silently keep
            // the first one instead of aborting pipeline initialization.
            if ( !weaverTypesBuilder.ContainsKey( weaverTypeName ) )
            {
                weaverTypesBuilder.Add( weaverTypeName, weaver );
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