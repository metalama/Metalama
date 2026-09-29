// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// Tests that the finalizer of <see cref="BaseEndpoint"/> never lets an exception escape. An exception thrown on the finalizer
/// thread is unhandled, so it terminates the host process.
/// </summary>
public sealed partial class EndpointFinalizerTests : RpcUnitTestClass
{
    public EndpointFinalizerTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Tests the finalizer of a <see cref="ServerEndpoint"/> whose constructor failed before it assigned any field.
    /// </summary>
    [Fact]
    public void Finalize_UninitializedServerEndpoint_DoesNotThrow()
    {
        var endpoint = (BaseEndpoint) RuntimeHelpers.GetUninitializedObject( typeof(TestServerEndpoint) );

        InvokeFinalizer( endpoint );
    }

    /// <summary>
    /// Tests the finalizer of a <see cref="ClientEndpoint"/> whose constructor failed before it assigned any field.
    /// </summary>
    [Fact]
    public void Finalize_UninitializedClientEndpoint_DoesNotThrow()
    {
        var endpoint = (BaseEndpoint) RuntimeHelpers.GetUninitializedObject( typeof(TestClientEndpoint) );

        InvokeFinalizer( endpoint );
    }

    /// <summary>
    /// Tests the finalizer of an endpoint whose <c>Dispose( false )</c> throws.
    /// </summary>
    [Fact]
    public void Finalize_DisposeThrows_DoesNotThrow()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(EndpointFinalizerTests)}_{Guid.NewGuid()}";

        using var endpoint = new ThrowingServerEndpoint( testContext.ServiceProvider, pipeName );

        InvokeFinalizer( endpoint );

        Assert.True( endpoint.DisposeWasCalledFromFinalizer );
    }

    /// <summary>
    /// Tests the finalizer of a <see cref="ServerEndpoint"/> and of a <see cref="ClientEndpoint"/> that were constructed
    /// but never disposed.
    /// </summary>
    [Fact]
    public void Finalize_ConstructedEndpoints_DoesNotThrow()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(EndpointFinalizerTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );
        using var clientEndpoint = new TestClientEndpoint( testContext.ServiceProvider, pipeName );

        InvokeFinalizer( serverEndpoint );
        InvokeFinalizer( clientEndpoint );
    }

    /// <summary>
    /// Calls the finalizer of an endpoint synchronously, so that an exception thrown by the finalizer fails the test
    /// instead of terminating the test process. The finalizer is then suppressed so that the garbage collector does not
    /// call it a second time.
    /// </summary>
    private static void InvokeFinalizer( BaseEndpoint endpoint )
    {
        var finalizer = endpoint.GetType().GetMethod( "Finalize", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null );

        Assert.NotNull( finalizer );
        Assert.Equal( typeof(BaseEndpoint), finalizer.DeclaringType );

        try
        {
            var exception = Record.Exception( () => finalizer.Invoke( endpoint, null ) );

            Assert.Null( (exception as TargetInvocationException)?.InnerException ?? exception );
        }
        finally
        {
            GC.SuppressFinalize( endpoint );
        }
    }
}
