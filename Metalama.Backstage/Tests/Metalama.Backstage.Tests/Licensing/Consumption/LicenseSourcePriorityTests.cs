// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tests.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

public sealed class LicenseSourcePriorityTests : LicensingTestsBase
{
    private const string _invalidProjectLicense = "invalid-project";

    private const string _invalidUserLicense = "invalid-user";

    private static readonly Predicate<LicenseConsumptionData> _testLicenseRequirement = _ => true;

    public LicenseSourcePriorityTests( ITestOutputHelper logger ) : base( logger ) { }

    protected override void ConfigureServices( ServiceProviderBuilder services ) { }

    private ILicenseConsumer CreateLicenseConsumer(
        bool isUnattendedProcess,
        string? projectLicense,
        string? userLicense,
        bool isPreview )
    {
        var serviceCollection = this.CloneServiceCollection();

        var serviceProviderBuilder =
            new ServiceCollectionBuilder( serviceCollection );

        serviceProviderBuilder.AddSingleton<IApplicationInfoProvider>(
                new ApplicationInfoProvider(
                    new TestApplicationInfo( "License Source Priority Test App", isPreview, "1.0.0", new DateTime( 2022, 1, 1, 0, 0, 0, DateTimeKind.Utc ) )
                    {
                        IsUnattendedProcess = isUnattendedProcess
                    } ) )
            .AddSingleton<IConfigurationManager>( serviceProvider => new Configuration.ConfigurationManager( serviceProvider ) );

        var serviceProvider = serviceCollection.BuildServiceProvider();

        if ( userLicense != null )
        {
            TestLicensingConfigurationHelpers.SetStoredLicenseString( serviceProvider, userLicense );
        }

        var options = new LicensingInitializationOptions();

        var service = LicenseConsumptionServiceFactory.Create( serviceProvider, options );

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
        // We don't pass an invalid project license, because project license disables unattended license.
        var licenseConsumptionManager = this.CreateLicenseConsumer( true, null, _invalidUserLicense, false );
        Assert.True( licenseConsumptionManager.TryConsume( _testLicenseRequirement ) );
        Assert.Empty( licenseConsumptionManager.Messages );
    }

    [Fact]
    public void ProjectLicenseHasPriorityOverUserLicense()
    {
        var licenseConsumptionManager = this.CreateLicenseConsumer( false, _invalidProjectLicense, _invalidUserLicense, false );
        Assert.False( licenseConsumptionManager.TryConsume( _testLicenseRequirement ) );
        Assert.Contains( _invalidProjectLicense, licenseConsumptionManager.Messages[0].Text, StringComparison.OrdinalIgnoreCase );
    }

    [Fact]
    public void UserLicenseHasPriorityOverPreviewLicense()
    {
        var licenseConsumptionManager = this.CreateLicenseConsumer( false, null, _invalidUserLicense, true );
        Assert.False( licenseConsumptionManager.TryConsume( _testLicenseRequirement ) );
        Assert.Contains( _invalidUserLicense, licenseConsumptionManager.Messages[0].Text, StringComparison.OrdinalIgnoreCase );
    }
}