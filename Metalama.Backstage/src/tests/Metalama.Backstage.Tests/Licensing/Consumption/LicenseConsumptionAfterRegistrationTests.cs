// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

/// <summary>
/// Tests that the <see cref="LicenseConsumptionService.Changed"/> event is raised
/// when a license is registered after the service was created.
/// Regression test for https://github.com/metalama/Metalama/issues/687.
/// </summary>
public sealed class LicenseConsumptionAfterRegistrationTests : LicensingTestsBase
{
    public LicenseConsumptionAfterRegistrationTests( ITestOutputHelper logger )
        : base( logger ) { }

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
