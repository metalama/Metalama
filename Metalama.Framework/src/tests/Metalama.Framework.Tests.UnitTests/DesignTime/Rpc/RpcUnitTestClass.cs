// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Testing.Hooks;
using Metalama.Testing.UnitTesting;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// Base class for RPC unit tests. Provides <see cref="CreateRpcTestContext"/> which returns an <see cref="RpcTestContext"/>
/// configured with <see cref="TestSynchronizationProvider"/>.
/// </summary>
public abstract class RpcUnitTestClass : UnitTestClass
{
    protected RpcUnitTestClass( ITestOutputHelper? logger = null ) : base( logger ) { }

    /// <summary>
    /// Creates an RPC test context with synchronization provider pre-configured.
    /// </summary>
    /// <param name="contextOptions">Optional non-default <see cref="TestContextOptions"/> (e.g. a shorter
    /// <see cref="TestContextOptions.Timeout"/>). When <c>null</c>, the default options are used.</param>
    /// <param name="callerFile">Automatically populated by the compiler.</param>
    /// <param name="callerMemberName">Automatically populated by the compiler.</param>
    /// <returns>An <see cref="RpcTestContext"/> that must be disposed after the test.</returns>
    [MustDisposeResource]
    private protected RpcTestContext CreateRpcTestContext(
        TestContextOptions? contextOptions = null,
        [CallerFilePath] string? callerFile = null,
        [CallerMemberName] string? callerMemberName = null )
    {
        var testContext = this.CreateTestContext( contextOptions, null, callerFile, callerMemberName );

        return new RpcTestContext( testContext, testContext.SyncProvider );
    }
}