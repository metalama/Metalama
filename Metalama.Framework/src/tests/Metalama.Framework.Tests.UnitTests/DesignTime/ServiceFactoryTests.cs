// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Contracts.Workspaces;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VisualStudio.Services;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime;

public sealed class ServiceFactoryTests : UnitTestClass
{
    public ServiceFactoryTests( ITestOutputHelper logger ) : base( logger ) { }

    private void TestGetServiceProvider( Func<DesignTimeEntryPointManager, DesignTimeServiceProviderFactory> createFactory )
    {
        using var testContext = this.CreateTestContext();
        var entryPointManager = new DesignTimeEntryPointManager();
        var factory = createFactory( entryPointManager );
        var serviceProvider = factory.GetServiceProvider( testContext.ServiceProvider.Global );
        serviceProvider.Dispose();
    }

    [Fact]
    public void UserProcess() => this.TestGetServiceProvider( entryPointManager => new DesignTimeUserProcessServiceProviderFactory( entryPointManager ) );

    [Fact]
    public void AnalysisProcess()
        => this.TestGetServiceProvider( entryPointManager => new DesignTimeAnalysisProcessServiceProviderFactory( entryPointManager ) );

    [Fact]
    public void VsUserProcess() => this.TestGetServiceProvider( entryPointManager => new VsUserProcessServiceProviderFactory( entryPointManager ) );

    [Fact]
    public void VsAnalysisProcess() => this.TestGetServiceProvider( entryPointManager => new VsAnalysisProcessServiceProviderFactory( entryPointManager ) );

    /// <summary>
    /// The Visual Studio extension can give the workspace of Visual Studio to the user process through the cross-version
    /// <see cref="IWorkspaceReceiver"/>, and the workspace provider of that process receives it.
    /// </summary>
    [Fact]
    public async Task VsUserProcessReceivesTheWorkspace()
    {
        using var testContext = this.CreateTestContext();
        var entryPointManager = new DesignTimeEntryPointManager();
        var factory = new VsUserProcessServiceProviderFactory( entryPointManager );
        using var serviceProvider = factory.GetServiceProvider( testContext.ServiceProvider.Global );

        var version = typeof(DesignTimeEntryPointManager).Assembly.GetName().Version!;
        var consumer = entryPointManager.GetConsumer( CurrentContractVersions.All );
        var compilerServiceProvider = await consumer.GetServiceProviderAsync( version, testContext.CancellationToken );
        Assert.NotNull( compilerServiceProvider );

        // ReSharper disable once SuspiciousTypeConversion.Global
        var workspaceReceiver = (IWorkspaceReceiver?) compilerServiceProvider.GetService( typeof(IWorkspaceReceiver) );
        Assert.NotNull( workspaceReceiver );

        using var workspace = new AdhocWorkspace();
        workspaceReceiver.SetWorkspace( workspace );

        GlobalServiceProvider globalServiceProvider = serviceProvider;
        Assert.True( globalServiceProvider.GetRequiredService<LocalWorkspaceProvider>().TryGetWorkspace( out var receivedWorkspace ) );
        Assert.Same( workspace, receivedWorkspace );
    }
}