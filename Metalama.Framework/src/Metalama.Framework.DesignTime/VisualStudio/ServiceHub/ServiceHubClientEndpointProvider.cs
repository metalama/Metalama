// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceHub;

internal sealed class ServiceHubClientEndpointProvider : IServiceHubClientEndpointProvider
{
    private readonly object _initializeLock = new();
    private readonly GlobalServiceProvider _serviceProvider;
    private ServiceHubClientEndpoint? _endpoint;

    public ServiceHubClientEndpointProvider( GlobalServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    public bool TryGetEndpoint( [NotNullWhen( true )] out ServiceHubClientEndpoint? endpoint )
    {
        if ( this._endpoint == null )
        {
            lock ( this._initializeLock )
            {
                if ( this._endpoint == null )
                {
                    if ( !ServiceHubClientEndpoint.TryStart( this._serviceProvider, out this._endpoint ) )
                    {
                        endpoint = null;

                        return false;
                    }
                }
            }
        }

        endpoint = this._endpoint;

        return true;
    }
}