// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.UserInterface.Toasts;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

// ReSharper disable once InconsistentNaming
public sealed class LicenseUITests : LicenseConsumptionServiceTestsBase
{
    public LicenseUITests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public async Task NotificationShownWhenMissingRequirement()
    {
        var consumer = this.CreateConsumptionService().CreateConsumer();
        Assert.False( consumer.TryConsume( new DelegateLicenseRequirement( _ => false ) ) );
        await this.DrainEventsAsync();
        Assert.NotEmpty( this.UserInterface.Notifications );
        Assert.Equal( ToastNotificationKinds.RequiresLicense, this.UserInterface.Notifications.Single().Kind );
    }

    [Fact]
    public async Task NotificationNotShownWhenFulfilledRequirement()
    {
        var consumer = this.CreateConsumptionService( LicenseKeyProvider.MetalamaProfessionalBusiness ).CreateConsumer();
        Assert.True( consumer.TryConsume( LicenseRequirement.Any ) );
        await this.DrainEventsAsync();
        Assert.Empty( this.UserInterface.Notifications );
    }
}