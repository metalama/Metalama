// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.UserInterface;

public sealed class ToastNotificationStatusServiceTests : TestsBase
{
    private readonly IToastNotificationStatusService _toastService;

    public ToastNotificationStatusServiceTests( ITestOutputHelper logger, IApplicationInfo? applicationInfo = null ) :
        base( logger, applicationInfo )
    {
        this._toastService = this.ServiceProvider.GetRequiredBackstageService<IToastNotificationStatusService>();
    }

    [Fact]
    public void AutoSnooze()
    {
        // The first time should succeed.
        Assert.True( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );

        // The second time should not.
        Assert.False( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );

        // Advance the clock.
        this.Time.AddTime( ToastNotificationKinds.LicenseExpiring.AutoSnoozePeriod );

        // Now this should work again.
        Assert.True( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );
    }

    [Fact]
    public void Disable()
    {
        this._toastService.Mute( ToastNotificationKinds.LicenseExpiring );
        Assert.False( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );
    }

    [Fact]
    public void Snooze()
    {
        // Snooze before anything.
        this._toastService.Snooze( ToastNotificationKinds.LicenseExpiring );

        // This should be snoozed.
        Assert.False( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );

        // Advance the clock to the auto snooze. It should still be snoozed.
        this.Time.AddTime( ToastNotificationKinds.LicenseExpiring.AutoSnoozePeriod );
        Assert.False( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );

        // Advance the clock to the manual snooze. Now this should work again.
        this.Time.AddTime( ToastNotificationKinds.LicenseExpiring.ManualSnoozePeriod );
        Assert.True( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );
    }

    [Fact]
    public void Pause()
    {
        var pausePeriod = TimeSpan.FromMinutes( 10 );
        this._toastService.PauseAll( pausePeriod );

        // This should be paused.
        Assert.False( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );

        this.Time.AddTime( pausePeriod );

        Assert.True( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );
    }

    [Fact]
    public void PauseAndResume()
    {
        var pausePeriod = TimeSpan.FromMinutes( 10 );

        var cookie = this._toastService.PauseAll( pausePeriod );

        // This should be paused.
        Assert.False( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );

        cookie.Dispose();

        Assert.True( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );
    }

    [Fact]
    public void PauseCleanUp()
    {
        var pausePeriod = TimeSpan.FromMinutes( 10 );
        this._toastService.PauseAll( pausePeriod );

        // This should be paused.
        Assert.False( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );

        Assert.Single( this.ConfigurationManager!.Get<ToastNotificationsConfiguration>().Pauses );

        this.Time.AddTime( pausePeriod );

        Assert.True( this._toastService.TryAcquire( ToastNotificationKinds.LicenseExpiring ) );

        this._toastService.PauseAll( pausePeriod );

        // We should have a single pause record and not 2 because we cleaned up.
        Assert.Single( this.ConfigurationManager!.Get<ToastNotificationsConfiguration>().Pauses );
    }
}