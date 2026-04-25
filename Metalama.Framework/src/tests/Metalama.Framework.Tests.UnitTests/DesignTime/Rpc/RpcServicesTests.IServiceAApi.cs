// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcServicesTests
{
    private interface IServiceAApi : IRpcApi
    {
        Task<string> GetServiceAValueAsync( CancellationToken cancellationToken );

        Task TriggerServiceAEventAsync( string message, CancellationToken cancellationToken );
    }
}