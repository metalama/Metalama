// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System;
using System.Threading;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// A test context specifically configured for RPC tests.
/// Provides access to <see cref="JsonSerializationBinderProvider"/> and <see cref="TestSynchronizationProvider"/>.
/// </summary>
internal sealed class RpcTestContext : IDisposable
{
    private readonly TestContext _testContext;

    /// <summary>
    /// Gets the JSON serialization binder provider for RPC tests.
    /// </summary>
    public JsonSerializationBinderProvider JsonSerializationBinderProvider { get; }

    /// <summary>
    /// Gets the test synchronization provider for deterministic race condition testing.
    /// </summary>
    public TestSynchronizationProvider SyncProvider { get; }

    /// <summary>
    /// Gets the underlying service provider configured with RPC services.
    /// Use this for tests that need a simple <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the global service provider configured with RPC services.
    /// Use this for tests that need to call methods on <see cref="GlobalServiceProvider"/> like <c>WithService</c>.
    /// </summary>
    public GlobalServiceProvider Global { get; }

    /// <summary>
    /// Gets a cancellation token that can be used to cancel operations in case of test timeout.
    /// </summary>
    public CancellationToken CancellationToken => this._testContext.CancellationToken;

    internal RpcTestContext( TestContext testContext, JsonSerializationBinderProvider jsonSerializationBinderProvider, TestSynchronizationProvider syncProvider )
    {
        this._testContext = testContext;
        this.JsonSerializationBinderProvider = jsonSerializationBinderProvider;
        this.SyncProvider = syncProvider;
        this.Global = testContext.ServiceProvider.Global;
        this.ServiceProvider = this.Global.Underlying;
    }

    public void Dispose()
    {
        // Always release all sync points to avoid deadlocks if the test fails.
        this.SyncProvider.ReleaseAll();
        this._testContext.Dispose();
    }
}
