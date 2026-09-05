// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Utilities;
using System;
using System.Linq;

namespace Metalama.Backstage.UserInterface.Toasts;

/// <summary>
/// The production implementation of <see cref="IToastNotificationStatusService"/>. 
/// </summary>
[PublicAPI]
public sealed class ToastNotificationStatusService : IToastNotificationStatusService
{
    private readonly IConfigurationManager _configurationManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger _logger;

    [PublicAPI]
    public ToastNotificationStatusService( IServiceProvider serviceProvider )
    {
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(ToastNotificationStatusService) );
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
            // A kind that cannot be muted ignores a stored mute. Such a mute can only have been written by a version
            // that still offered the Mute button, and it would otherwise keep the notification silenced for good. See #1751.
            if ( !kind.CanBeMuted )
            {
                this._logger.Trace?.Log( $"The notification kind {kind.Name} is marked as disabled, but this kind cannot be muted." );

                return true;
            }

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

    /// <summary>
    /// Returns the value to store for a notification kind, after copying into it the members of the previous value
    /// that this version of Metalama does not declare.
    /// </summary>
    /// <remarks>
    /// An update of a notification kind builds a new value instead of copying the previous one with a <c>with</c>
    /// expression, because the declared members are reset. The members written by a newer version of Metalama are
    /// therefore carried over explicitly, otherwise the update would remove them from the configuration file.
    /// See <see cref="ConfigurationObject.CopyUnknownMembersFrom"/>.
    /// </remarks>
    /// <param name="configuration">The configuration file before the update.</param>
    /// <param name="kind">The kind of notification whose value is replaced.</param>
    /// <param name="newValue">The new value, which is modified in place and returned.</param>
    private static ToastNotificationConfiguration WithUnknownMembersOfPreviousValue(
        ToastNotificationsConfiguration configuration,
        ToastNotificationKind kind,
        ToastNotificationConfiguration newValue )
    {
        configuration.Notifications.TryGetValue( kind.Name, out var previousValue );
        newValue.CopyUnknownMembersFrom( previousValue );

        return newValue;
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
                // Record the time so that throttled (low-priority) notifications are deferred after any notification.
                LastNotificationTime = this._dateTimeProvider.UtcNow,
                Notifications = c.Notifications.SetItem(
                    kind.Name,
                    WithUnknownMembersOfPreviousValue(
                        c,
                        kind,
                        new ToastNotificationConfiguration { SnoozeUntil = this._dateTimeProvider.UtcNow + kind.AutoSnoozePeriod } ) )
            } );
    }

    public void Snooze( ToastNotificationKind kind )
        => this._configurationManager.Update<ToastNotificationsConfiguration>(
            config => config with
            {
                Notifications = config.Notifications.SetItem(
                    kind.Name,
                    WithUnknownMembersOfPreviousValue(
                        config,
                        kind,
                        new ToastNotificationConfiguration { SnoozeUntil = this._dateTimeProvider.UtcNow + kind.ManualSnoozePeriod } ) )
            } );

    public void Mute( ToastNotificationKind kind )
    {
        if ( !kind.CanBeMuted )
        {
            this._logger.Trace?.Log( $"The notification kind {kind.Name} cannot be muted." );

            return;
        }

        this._configurationManager.Update<ToastNotificationsConfiguration>(
            config => config with
            {
                Notifications = config.Notifications.SetItem(
                    kind.Name,
                    WithUnknownMembersOfPreviousValue( config, kind, new ToastNotificationConfiguration { Disabled = true } ) )
            } );
    }

    public IDisposable PauseAll( TimeSpan timeSpan )
    {
        var id = Guid.NewGuid().ToString();

        // We clean up non-disposed pauses, and we add our.

        this._configurationManager.Update<ToastNotificationsConfiguration>(
            config => config with
            {
                Pauses = config.Pauses

                    // Use '<=' so a pause that expires exactly at the current time is cleaned up. This matches the
                    // 'IsPaused' check (which treats 'Value > now' as active) and avoids accumulating stale pauses.
                    .RemoveRange( config.Pauses.Where( c => c.Value <= this._dateTimeProvider.UtcNow ).Select( c => c.Key ) )
                    .Add( id, this._dateTimeProvider.UtcNow.Add( timeSpan ) )
            } );

        return new DisposableAction( Resume );

        void Resume()
        {
            this._configurationManager.Update<ToastNotificationsConfiguration>( config => config with { Pauses = config.Pauses.Remove( id ) } );
        }
    }

    public DateTime? LastNotificationTime => this._configurationManager.Get<ToastNotificationsConfiguration>().LastNotificationTime;

    private bool IsPaused
    {
        get
        {
            var pauses = this._configurationManager.Get<ToastNotificationsConfiguration>().Pauses;

            return pauses.Any( p => p.Value > this._dateTimeProvider.UtcNow );
        }
    }

    public static TimeSpan LowPriorityThrottlePeriod => TimeSpan.FromMinutes( 15 );

    public bool CanDisplayLowPriorityNotifications
    {
        get
        {
            var lastNotificationTime = this.LastNotificationTime;

            return lastNotificationTime == null || lastNotificationTime < this._dateTimeProvider.UtcNow - LowPriorityThrottlePeriod;
        }
    }
}