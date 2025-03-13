// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.UserInterface;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

public sealed class LicenseUITests : LicenseConsumptionServiceTestsBase
{
    public LicenseUITests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void NotificationShownWhenMissingRequirement()
    {
        var consumer = this.CreateConsumptionService().CreateConsumer();
        Assert.False( consumer.TryConsume( new DelegateLicenseRequirement( _ => false ) ) );
        Assert.NotEmpty( this.UserInterface.Notifications );
        Assert.Equal( ToastNotificationKinds.RequiresLicense, this.UserInterface.Notifications.Single().Kind );
    }

    [Fact]
    public void NotificationNotShownWhenFulfilledRequirement()
    {
        var consumer = this.CreateConsumptionService( LicenseKeyProvider.MetalamaProfessionalBusiness ).CreateConsumer();
        Assert.True( consumer.TryConsume( LicenseRequirement.Any ) );
        Assert.Empty( this.UserInterface.Notifications );
    }
}