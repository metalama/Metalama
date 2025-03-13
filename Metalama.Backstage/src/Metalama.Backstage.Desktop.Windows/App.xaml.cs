// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Desktop.Windows.Commands;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Microsoft.Toolkit.Uwp.Notifications;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Metalama.Backstage.Desktop.Windows;

internal partial class App
{
    public App()
    {
        this.DispatcherUnhandledException += OnUnhandledException;
    }

    private static void OnUnhandledException( object sender, DispatcherUnhandledExceptionEventArgs e )
    {
        MessageBox.Show( e.Exception.Message );
    }

    public static IServiceProvider GetBackstageServices( BaseSettings settings )
    {
        BackstageServiceFactory.Initialize(
            new BackstageInitializationOptions( new DesktopWindowsApplicationInfo() )
            {
                AddLicensing = false,
                IsDevelopmentEnvironment = settings.IsDevelopmentEnvironment,
                AddSupportServices = true,
                AddUserInterface = true,

                // We don't want to open more toast notifications.
                DetectToastNotifications = false
            },
            "Metalama.Backstage.Desktop.Windows" );

        var logger = BackstageServiceFactory.ServiceProvider.GetLoggerFactory().GetLogger( "App" );
        logger.Trace?.Log( $"Executing: {string.Join( ' ', Environment.GetCommandLineArgs() )}" );

        return BackstageServiceFactory.ServiceProvider;
    }

    private static Task RunAppAsync( IEnumerable<string> args )
    {
        // Make sure to run the app in a background thread.
        return Task.Run(
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
                    } );

                return commandApp.RunAsync( args );
            } );
    }

    protected override void OnStartup( StartupEventArgs e )
    {
        base.OnStartup( e );

        ToastNotificationManagerCompat.OnActivated += OnToastNotificationActivated;

        if ( !ToastNotificationManagerCompat.WasCurrentProcessToastActivated() )
        {
            RunAppAsync( e.Args )
                .ContinueWith( _ => this.Dispatcher.BeginInvoke( () => this.Shutdown() ) );
        }
    }

    private static void OnToastNotificationActivated( ToastNotificationActivatedEventArgsCompat e )
    {
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