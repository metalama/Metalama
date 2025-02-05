// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Utilities;
using System;
using System.Linq;

namespace Metalama.Backstage.UserInterface;

/// <summary>
/// The production implementation of <see cref="IToastNotificationStatusService"/>. 
/// </summary>
public class ToastNotificationStatusService : IToastNotificationStatusService
{
    private readonly IConfigurationManager _configurationManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger _logger;

    public ToastNotificationStatusService( IServiceProvider serviceProvider )
    {
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
    }

    private bool IsEnabled( ToastNotificationKind kind, ToastNotificationsConfiguration configuration )
    {
        if ( !configuration.Notifications.TryGetValue( kind.Name, out var kindConfiguration ) )
        {
            this._logger.Trace?.Log( $"The notification kind {kind.Name} is not configured." );

            // A notification is enabled by default.
            return true;
        }

        if ( kindConfiguration.Disabled )
        {
            this._logger.Trace?.Log( $"The notification kind {kind.Name} is disabled." );

            return false;
        }

        if ( kindConfiguration.SnoozeUntil != null && kindConfiguration.SnoozeUntil > this._dateTimeProvider.UtcNow )
        {
            this._logger.Trace?.Log( $"The notification kind {kind.Name} is snoozed until {kindConfiguration.SnoozeUntil}." );

            return false;
        }

        this._logger.Trace?.Log( $"The notification kind {kind.Name} is active." );

        return true;
    }

    public bool TryAcquire( ToastNotificationKind kind )
    {
        if ( this.IsPaused )
        {
            this._logger.Trace?.Log( "Notifications are paused." );

            return false;
        }

        return this._configurationManager.UpdateIf<ToastNotificationsConfiguration>(
            c => this.IsEnabled( kind, c ),
            c => c with
            {
                Notifications = c.Notifications.SetItem(
                    kind.Name,
                    new ToastNotificationConfiguration() { SnoozeUntil = this._dateTimeProvider.UtcNow + kind.AutoSnoozePeriod } )
            } );
    }

    public void Snooze( ToastNotificationKind kind )
        => this._configurationManager.Update<ToastNotificationsConfiguration>(
            config => config with
            {
                Notifications = config.Notifications.SetItem(
                    kind.Name,
                    new ToastNotificationConfiguration { SnoozeUntil = this._dateTimeProvider.UtcNow + kind.ManualSnoozePeriod } )
            } );

    public void Mute( ToastNotificationKind kind )
        => this._configurationManager.Update<ToastNotificationsConfiguration>(
            config => config with
            {
                Notifications = config.Notifications.SetItem(
                    kind.Name,
                    new ToastNotificationConfiguration { Disabled = true } )
            } );

    public IDisposable PauseAll( TimeSpan timeSpan )
    {
        var id = Guid.NewGuid().ToString();

        // We clean up non-disposed pauses, and we add our.

        this._configurationManager.Update<ToastNotificationsConfiguration>(
            config => config with
            {
                Pauses = config.Pauses
                    .RemoveRange( config.Pauses.Where( c => c.Value < this._dateTimeProvider.UtcNow ).Select( c => c.Key ) )
                    .Add( id, this._dateTimeProvider.UtcNow.Add( timeSpan ) )
            } );

        return new DisposableAction( Resume );

        void Resume()
        {
            this._configurationManager.Update<ToastNotificationsConfiguration>( config => config with { Pauses = config.Pauses.Remove( id ) } );
        }
    }

    private bool IsPaused
    {
        get
        {
            var pauses = this._configurationManager.Get<ToastNotificationsConfiguration>().Pauses;

            return pauses.Any( p => p.Value > this._dateTimeProvider.UtcNow );
        }
    }
}