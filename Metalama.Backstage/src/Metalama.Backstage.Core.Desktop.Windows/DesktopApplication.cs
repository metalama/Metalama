// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Desktop.Windows.Commands;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Microsoft.Toolkit.Uwp.Notifications;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Metalama.Backstage.Desktop.Windows;

/// <summary>
/// The WPF application of the notifier. It runs the command given on the command line, or the command carried by
/// the toast that activated the process, and exits.
/// </summary>
internal sealed class DesktopApplication : System.Windows.Application
{
    private readonly string[] _args;

    public DesktopApplication( string[] args )
    {
        this._args = args;
        this.DispatcherUnhandledException += OnUnhandledException;
    }

    private static void OnUnhandledException( object sender, DispatcherUnhandledExceptionEventArgs e )
    {
        MessageBox.Show( e.Exception.Message );
    }

    private static async Task RunAppAsync( IEnumerable<string> args )
    {
        // Make sure to run the app in a background thread.
        await Task.Run(
            () =>
            {
                CommandApp commandApp = new();

                commandApp.Configure(
                    configuration =>
                    {
                        configuration.AddCommand<NotifyCommand>( NotifyCommand.Name );
                        configuration.AddCommand<SnoozeNotificationCommand>( SnoozeNotificationCommand.Name );
                        configuration.AddCommand<MuteNotificationCommand>( MuteNotificationCommand.Name );
                        configuration.AddCommand<SetupWizardCommand>( SetupWizardCommand.Name );
                        configuration.AddCommand<OpenWorkerRssOptionsCommand>( OpenWorkerRssOptionsCommand.Name );
                        configuration.AddCommand<OpenWorkerPrivacyOptionsCommand>( OpenWorkerPrivacyOptionsCommand.Name );
                        configuration.AddCommand<OpenExceptionReportCommand>( OpenExceptionReportCommand.Name );
                        configuration.AddCommand<ReportExceptionCommand>( ReportExceptionCommand.Name );
                    } );

                return commandApp.RunAsync( args );
            } );

        // A command returns as soon as it has done its work, but it may have enqueued background work: sending a report
        // starts the upload process that way. This process exits immediately afterwards (through Environment.Exit for a
        // toast activation), which would kill a task that has not started yet, so we wait for the queue to drain here
        // rather than rely on the ProcessExit handler having time to run. See #1751.
        var serviceProvider = DesktopServices.ServiceProvider;

        if ( serviceProvider != null )
        {
            await serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>().CompleteAsync();
        }
    }

    protected override void OnStartup( StartupEventArgs e )
    {
        base.OnStartup( e );

        ToastNotificationManagerCompat.OnActivated += OnToastNotificationActivated;

        if ( !ToastNotificationManagerCompat.WasCurrentProcessToastActivated() )
        {
            RunAppAsync( this._args )
                .ContinueWith( _ => this.Dispatcher.BeginInvoke( () => this.Shutdown() ) );
        }
    }

    private static void OnToastNotificationActivated( ToastNotificationActivatedEventArgsCompat e )
    {
        // If the argument is a URL, open it in the browser. This handles the case where
        // protocol activation from toast buttons is routed through the COM activator.
        if ( Uri.TryCreate( e.Argument, UriKind.Absolute, out var uri )
             && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) )
        {
            try
            {
                Process.Start( new ProcessStartInfo( e.Argument ) { UseShellExecute = true } );
            }
            catch ( Exception ex )
            {
                // The services may not have been created yet, because no command has run in this process.
                var logger = DesktopServices.ServiceProvider?.GetLoggerFactory().GetLogger( "App" );
                logger?.Error?.Log( $"Failed to launch browser for toast activation URL '{e.Argument}': {ex}" );
            }
            finally
            {
                ToastNotificationManagerCompat.History.Clear();
                Environment.Exit( 0 );
            }

            return;
        }

        RunAppAsync( e.Argument.Split( ' ' ) )
            .ContinueWith(
                _ =>
                {
                    // Remove all notifications from this app.
                    ToastNotificationManagerCompat.History.Clear();

                    // Calling Shutdown does not seem to work.
                    Environment.Exit( 0 );
                } );
    }
}
