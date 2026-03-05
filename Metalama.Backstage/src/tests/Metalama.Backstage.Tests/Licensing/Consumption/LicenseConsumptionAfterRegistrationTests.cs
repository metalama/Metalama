// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Requirements;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

/// <summary>
/// Tests that the <see cref="LicenseConsumptionService"/> correctly picks up license changes
/// when a license is registered after the service was created.
/// Regression test for https://github.com/metalama/Metalama/issues/687.
/// </summary>
public sealed class LicenseConsumptionAfterRegistrationTests : LicensingTestsBase
{
    public LicenseConsumptionAfterRegistrationTests( ITestOutputHelper logger )
        : base( logger ) { }

    /// <summary>
    /// Verifies that a <see cref="LicenseConsumptionService"/> using a <see cref="UserProfileLicenseSource"/>
    /// correctly detects a newly registered license when a new consumer is created.
    /// This simulates the scenario where the user registers a license while the IDE is running.
    /// </summary>
    [Fact]
    public void ConsumptionServiceDetectsNewlyRegisteredLicense()
    {
        // Arrange: Create a consumption service with a UserProfileLicenseSource (but no license registered yet).
        var userProfileSource = new UserProfileLicenseSource( this.ServiceProvider );
        ILicenseConsumptionService consumptionService = new LicenseConsumptionService( this.ServiceProvider, new ILicenseSource[] { userProfileSource } );
        var requirement = new MetalamaExtensionLicenseRequirement( "TestExtension" );

        // Act 1: Try to consume the license BEFORE registration — should fail.
        var consumerBeforeRegistration = consumptionService.CreateConsumer();
        var canConsumeBeforeRegistration = consumerBeforeRegistration.TryConsume( requirement );

        // Assert 1: No license registered, so consumption should fail.
        Assert.False( canConsumeBeforeRegistration, "Expected license consumption to fail before a license is registered." );

        // Act 2: Register a valid professional license.
        var licenseKey = LicenseKeyProvider.MetalamaProfessionalBusiness;
        var registrationResult = this.LicenseRegistrationService.RegisterLicense( licenseKey );
        Assert.True( registrationResult.IsSuccess, "License registration failed." );

        // Act 3: Create a NEW consumer from the same consumption service and try again.
        var consumerAfterRegistration = consumptionService.CreateConsumer();
        var canConsumeAfterRegistration = consumerAfterRegistration.TryConsume( requirement );

        // Assert 3: After registration, the new consumer should detect the license.
        Assert.True(
            canConsumeAfterRegistration,
            "Expected license consumption to succeed after a license is registered. "
            + "The LicenseConsumptionService should detect the newly registered license via the UserProfileLicenseSource." );
    }

    /// <summary>
    /// Verifies that the <see cref="LicenseConsumptionService.Changed"/> event is raised
    /// when a license is registered, allowing listeners to invalidate cached state.
    /// </summary>
    [Fact]
    public void ConsumptionServiceRaisesChangedEventOnLicenseRegistration()
    {
        // Arrange: Create a consumption service with a UserProfileLicenseSource.
        var userProfileSource = new UserProfileLicenseSource( this.ServiceProvider );
        ILicenseConsumptionService consumptionService = new LicenseConsumptionService( this.ServiceProvider, new ILicenseSource[] { userProfileSource } );

        var changedEventRaised = false;
        consumptionService.Changed += () => changedEventRaised = true;

        // Act: Register a license.
        var licenseKey = LicenseKeyProvider.MetalamaProfessionalBusiness;
        var registrationResult = this.LicenseRegistrationService.RegisterLicense( licenseKey );
        Assert.True( registrationResult.IsSuccess, "License registration failed." );

        // Assert: The Changed event should have been raised.
        Assert.True(
            changedEventRaised,
            "Expected the LicenseConsumptionService.Changed event to be raised when a license is registered." );
    }
}
