// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tools;
using Metalama.Backstage.UserInterface;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Metalama.Backstage.Tests.Extensibility;

public class RegisterServiceExtensionsTests
{
    private static ServiceCollectionBuilder CreateServiceCollectionBuilder() => new();

    [Theory]
    [InlineData( true, true, true )]
    [InlineData( false, true, true )]
    [InlineData( false, false, true )]
    [InlineData( true, true, false )]
    [InlineData( false, true, false )]
    [InlineData( false, false, false )]
    [InlineData( true, false, true, true )]
    [InlineData( true, true, true, false, false )]
    public void AddBackstageServices(
        bool addLicensing,
        bool addSupportServices,
        bool addUserInterface,
        bool disableLicenseAudit = true,
        bool addToolsExtractor = true )
    {
        var options =
            new BackstageInitializationOptions(
                new TestApplicationInfo( "Test", true, "1.0", DateTime.Today ) { IsLicenseAuditEnabled = !disableLicenseAudit } )
            {
                AddLicensing = addLicensing, AddSupportServices = addSupportServices, AddUserInterface = addUserInterface
            };

        if ( addToolsExtractor && (addSupportServices || addUserInterface) )
        {
            options = options with { AddToolsExtractor = b => b.AddService( typeof(IBackstageToolsExtractor), p => new BackstageToolsExtractor( p ) ) };
        }

        var serviceProviderBuilder = CreateServiceCollectionBuilder();
        serviceProviderBuilder.AddBackstageServices( options );
        var serviceProvider = serviceProviderBuilder.ServiceCollection.BuildServiceProvider();
        Assert.NotNull( serviceProvider.GetBackstageService<IPlatformInfo>() );

        if ( addLicensing )
        {
            Assert.NotNull( serviceProvider.GetBackstageService<ILicenseConsumptionService>() );

            if ( disableLicenseAudit )
            {
                Assert.Null( serviceProvider.GetBackstageService<ILicenseAuditManager>() );
            }
            else
            {
                Assert.NotNull( serviceProvider.GetBackstageService<ILicenseAuditManager>() );
            }
        }
        else
        {
            Assert.Null( serviceProvider.GetBackstageService<ILicenseConsumptionService>() );
            Assert.Null( serviceProvider.GetBackstageService<ILicenseAuditManager>() );
        }

        if ( addSupportServices )
        {
            Assert.NotNull( serviceProvider.GetBackstageService<ILoggerFactory>() );
            Assert.NotNull( serviceProvider.GetBackstageService<ITelemetryUploader>() );
            Assert.NotNull( serviceProvider.GetBackstageService<IExceptionReporter>() );
            Assert.NotNull( serviceProvider.GetBackstageService<IUsageReporter>() );
        }
        else
        {
            Assert.Null( serviceProvider.GetBackstageService<ILoggerFactory>() );
            Assert.Null( serviceProvider.GetBackstageService<IExceptionReporter>() );
            Assert.Null( serviceProvider.GetBackstageService<IUsageReporter>() );
            Assert.Null( serviceProvider.GetBackstageService<ITelemetryUploader>() );
        }

        if ( addUserInterface )
        {
            Assert.NotNull( serviceProvider.GetBackstageService<IToastNotificationService>() );
            Assert.NotNull( serviceProvider.GetBackstageService<IUserInterfaceService>() );
            Assert.NotNull( serviceProvider.GetBackstageService<IToastNotificationDetectionService>() );
        }
        else
        {
            Assert.Null( serviceProvider.GetBackstageService<IToastNotificationService>() );
            Assert.Null( serviceProvider.GetBackstageService<IUserInterfaceService>() );
            Assert.Null( serviceProvider.GetBackstageService<IToastNotificationDetectionService>() );
        }

        if ( addToolsExtractor && (addSupportServices || addUserInterface) )
        {
            Assert.NotNull( serviceProvider.GetBackstageService<IBackstageToolsExtractor>() );
        }
        else
        {
            Assert.Null( serviceProvider.GetBackstageService<IBackstageToolsExtractor>() );
        }
    }
}