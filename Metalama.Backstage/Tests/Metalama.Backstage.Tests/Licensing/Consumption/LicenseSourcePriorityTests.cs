// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
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

    private static readonly Predicate<LicenseConsumptionProperties> _testLicenseRequirement = _ => true;

    public LicenseSourcePriorityTests( ITestOutputHelper logger ) : base( logger ) { }

    protected override void ConfigureServices( ServiceProviderBuilder services ) { }

    private ILicenseConsumer CreateLicenseConsumer(
        bool isUnattendedProcess,
        string? projectLicense,
        string? userLicense,
        bool isPreview )
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
            Assert.True( this.LicenseRegistrationService.TryRegisterLicense( userLicense, out _ ) );
        }

        var service = serviceProvider.GetRequiredBackstageService<ILicenseConsumptionService>();

        return service.CreateConsumer( new LicenseConsumptionOptions { ProjectLicenseKey = projectLicense } );
    }

    [Fact]
    public void NoMessageGivenWithNoLicense()
    {
        var licenseConsumptionManager = this.CreateLicenseConsumer( false, null, null, false );
        Assert.False( licenseConsumptionManager.TryConsume( _testLicenseRequirement ) );
        Assert.Empty( licenseConsumptionManager.Messages );
    }

    [Fact]
    public void UnattendedLicenseHasHighestPriority()
    {
        var licenseConsumptionManager = this.CreateLicenseConsumer( true, null, UserLicense, false );
        Assert.True( licenseConsumptionManager.TryConsume( properties => properties.LicenseType == LicenseType.Unattended ) );
    }

    [Fact]
    public void ProjectLicenseHasPriorityOverUserLicense()
    {
        var licenseConsumptionManager = this.CreateLicenseConsumer( false, ProjectLicense, UserLicense, false );
        Assert.True( licenseConsumptionManager.TryConsume( properties => properties.LicenseString == ProjectLicense ) );
    }
}