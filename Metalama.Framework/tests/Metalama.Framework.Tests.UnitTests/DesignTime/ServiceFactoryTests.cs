// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VisualStudio.Services;
using Metalama.Testing.UnitTesting;
using System;
using Xunit;
using Xunit.Abstractions;

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
}