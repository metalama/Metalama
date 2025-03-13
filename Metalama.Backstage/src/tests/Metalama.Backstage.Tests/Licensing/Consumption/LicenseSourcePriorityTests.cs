// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tests.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

public sealed class LicenseSourcePriorityTests : LicensingTestsBase
{
    private static string ProjectLicense => LicenseKeyProvider.MetalamaProfessionalBusiness;

    private static string UserLicense => LicenseKeyProvider.MetalamaProfessionalPersonal;

    public LicenseSourcePriorityTests( ITestOutputHelper logger ) : base( logger ) { }

    protected override void ConfigureServices( ServiceProviderBuilder services ) { }

    private ILicenseConsumer CreateLicenseConsumer(
        bool isUnattendedProcess,
        string? projectLicense,
        string? userLicense,
        bool isPreview,
        Action<LicensingMessage>? reportMessage = null )
    {
        var serviceCollection = this.CloneServiceCollection();

        var serviceProviderBuilder = new ServiceCollectionBuilder( serviceCollection );

        serviceProviderBuilder.AddSingleton<IApplicationInfoProvider>(
            new ApplicationInfoProvider(
                new TestApplicationInfo( "License Source Priority Test App", isPreview, "1.0.0", new DateTime( 2022, 1, 1, 0, 0, 0, DateTimeKind.Utc ) )
                {
                    IsUnattendedProcess = isUnattendedProcess
                } ) );

        serviceProviderBuilder.AddSingleton<ILicenseConsumptionService>(
            sp =>
            {
                var licenseSources = new List<ILicenseSource> { new UnattendedLicenseSource( sp ), new UserProfileLicenseSource( sp ) };

                return new LicenseConsumptionService( sp, licenseSources );
            } );

        var serviceProvider = serviceCollection.BuildServiceProvider();

        if ( userLicense != null )
        {
            Assert.True( this.LicenseRegistrationService.RegisterLicense( userLicense ).IsSuccess );
        }

        var service = serviceProvider.GetRequiredBackstageService<ILicenseConsumptionService>();

        return service.CreateConsumer( new LicenseConsumptionOptions { ProjectLicenseKey = projectLicense }, reportMessage );
    }

    [Fact]
    public void NoMessageGivenWithNoLicense()
    {
        var hasMessage = false;
        this.CreateLicenseConsumer( false, null, null, false, _ => hasMessage = true );
        Assert.False( hasMessage );
        
        // Note that trying to consume does report a message in this case.
    }

    [Fact]
    public void UnattendedLicenseHasHighestPriority()
    {
        var licenseConsumptionManager = this.CreateLicenseConsumer( true, null, UserLicense, false );

        Assert.True(
            licenseConsumptionManager.TryConsume( new DelegateLicenseRequirement( context => context.License.LicenseType == LicenseType.Unattended ) ) );
    }

    [Fact]
    public void ProjectLicenseHasPriorityOverUserLicense()
    {
        var licenseConsumptionManager = this.CreateLicenseConsumer( false, ProjectLicense, UserLicense, false );
        Assert.True( licenseConsumptionManager.TryConsume( new DelegateLicenseRequirement( context => context.License.LicenseString == ProjectLicense ) ) );
    }
}