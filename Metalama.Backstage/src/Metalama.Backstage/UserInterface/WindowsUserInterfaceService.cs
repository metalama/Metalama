// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Tools;
using System;

#if NETFRAMEWORK || NETCOREAPP
using Metalama.Backstage.Diagnostics;
using Microsoft.Win32;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
#endif

namespace Metalama.Backstage.UserInterface;

#pragma warning disable CA1416

internal sealed class WindowsUserInterfaceService : UserInterfaceService
{
    private readonly IBackstageToolsExecutor _toolsExecutor;

    public WindowsUserInterfaceService( IServiceProvider serviceProvider ) : base( serviceProvider )
    {
        this._toolsExecutor = serviceProvider.GetRequiredBackstageService<IBackstageToolsExecutor>();
    }

    public override void ShowToastNotification( ToastNotification notification )
    {
        // Build arguments.
        var arguments = $"notify {notification.Kind.Name}";

        if ( !string.IsNullOrEmpty( notification.Title ) )
        {
            arguments += $" --title \"{notification.Title}\"";
        }

        if ( !string.IsNullOrEmpty( notification.Text ) )
        {
            arguments += $" --text \"{notification.Text}\"";
        }

        if ( !string.IsNullOrEmpty( notification.Uri ) )
        {
            arguments += $" --uri \"{notification.Uri}\"";
        }

        try
        {
            // Start the UI process.
            this._toolsExecutor.Start( BackstageTool.DesktopWindows, arguments );
            this.OnToastNotificationShown();
        }
        catch ( Exception e )
        {
            this.Logger.Error?.Log( "Cannot show notification: " + e.Message );
        }
    }

#if NETFRAMEWORK || NETCOREAPP
    protected override ProcessStartInfo GetProcessStartInfoForUrl( string url, BrowserMode browserMode )
    {
        if ( browserMode == BrowserMode.Default || !this.TryGetDefaultBrowser( out var browserPath ) )
        {
            return base.GetProcessStartInfoForUrl( url, browserMode );
        }

        switch ( Path.GetFileNameWithoutExtension( browserPath ) )
        {
            // ReSharper disable StringLiteralTypo
            case "msedge": // --window-size does not seem to work on Edge
            case "chrome":
            case "opera":
            case "brave":
            case "vivaldi":
            case "blisk":
            case "browser":
                // ReSharper restore StringLiteralTypo

                // For Chromium-based browsers, we know how to open the page in a new window or in an app window.
                var arg = browserMode switch
                {
                    BrowserMode.Application => $"--app={url} --window-size=820,940 --allow-insecure-localhost",
                    BrowserMode.NewWindow => $"--new-window {url}",
                    _ => throw new ArgumentOutOfRangeException( nameof(browserMode) )
                };

                return new ProcessStartInfo( browserPath, arg );

            default:
                return base.GetProcessStartInfoForUrl( url, browserMode );
        }
    }

    private bool TryGetDefaultBrowser( [NotNullWhen( true )] out string? path )
    {
        try
        {
            using ( var userChoiceKey = Registry.CurrentUser.OpenSubKey( @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice" ) )
            {
                var progIdValue = userChoiceKey?.GetValue( "ProgId" );

                if ( progIdValue == null )
                {
                    path = null;

                    return false;
                }

                var browser = progIdValue.ToString();

                using ( var progIdKey = Registry.ClassesRoot.OpenSubKey( browser + @"\shell\open\command" ) )
                {
#pragma warning disable CA1307

                    // ReSharper disable once RedundantSuppressNullableWarningExpression
                    path = progIdKey?.GetValue( null )?.ToString()!.Replace( "\"", "" );
#pragma warning restore CA1307

                    if ( string.IsNullOrEmpty( path ) )
                    {
                        return false;
                    }

                    // Handling paths with arguments. That's not bullet proof but this should be enough.
                    // ReSharper disable once RedundantSuppressNullableWarningExpression
                    if ( !path!.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase ) )
                    {
                        path = path.Substring( 0, path.LastIndexOf( ".exe", StringComparison.OrdinalIgnoreCase ) + 4 );
                    }

                    return true;
                }
            }
        }
        catch ( Exception exception )
        {
            this.Logger.LogException( exception );
            path = null;

            return false;
        }
    }
#endif
}