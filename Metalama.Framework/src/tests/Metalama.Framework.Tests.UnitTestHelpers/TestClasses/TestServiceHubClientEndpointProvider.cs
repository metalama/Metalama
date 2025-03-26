// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Tests.UnitTestHelpers.TestClasses;

internal class TestServiceHubClientEndpointProvider : IServiceHubClientEndpointProvider
{
    private readonly ServiceHubClientEndpoint _endpoint;

    public TestServiceHubClientEndpointProvider( ServiceHubClientEndpoint endpoint )
    {
        this._endpoint = endpoint;
    }

    public bool TryGetEndpoint( [NotNullWhen( true )] out ServiceHubClientEndpoint? endpoint )
    {
        endpoint = this._endpoint;

        return true;
    }
}