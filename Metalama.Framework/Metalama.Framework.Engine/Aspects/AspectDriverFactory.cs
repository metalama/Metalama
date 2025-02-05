// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using System.Collections.Immutable;

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

        this._weaverTypes = plugins.OfType<IAspectDriver>()
            .ToImmutableDictionary( weaver => weaver.GetType().FullName.AssertNotNull() );
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