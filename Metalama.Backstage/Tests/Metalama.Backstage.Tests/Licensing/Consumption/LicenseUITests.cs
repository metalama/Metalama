// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        Assert.True( consumer.TryConsume( new DelegateLicenseRequirement( _ => true ) ) );
        Assert.Empty( this.UserInterface.Notifications );
    }
}