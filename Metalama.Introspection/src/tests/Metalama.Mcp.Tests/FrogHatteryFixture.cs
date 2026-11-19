// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Mcp.Tests;

/// <summary>
/// A server with the FrogHattery sample loaded, shared by the tests of a class because loading takes seconds.
/// </summary>
public sealed class FrogHatteryFixture : IAsyncLifetime
{
    internal ServerProcess Server { get; private set; } = null!;

    internal string LoadText { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        this.Server = await ServerProcess.StartAsync();
        this.LoadText = await this.Server.CallSuccessfullyAsync( "load_workspace", new Dictionary<string, object?> { ["paths"] = new[] { ServerProcess.FrogHatteryPath } } );
    }

    public async ValueTask DisposeAsync() => await this.Server.DisposeAsync();
}
