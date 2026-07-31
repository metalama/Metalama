// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Tools;
using Metalama.Backstage.UserInterface.Toasts;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Metalama.Backstage.UserInterface;

[PublicAPI]
public abstract class UserInterfaceService : IUserInterfaceService
{
    private readonly IProcessExecutor _processExecutor;

    protected ILogger Logger { get; }

    private readonly IBackstageToolsExecutor _backstageToolExecutor;
    private readonly bool _canIgnoreRecoverableExceptions;
    private readonly IStandardDirectories _standardDirectories;
    private readonly RandomNumberGenerator _randomNumberGenerator;

    protected UserInterfaceService( IServiceProvider serviceProvider )
    {
        this._processExecutor = serviceProvider.GetRequiredBackstageService<IProcessExecutor>();
        this.Logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
        this._backstageToolExecutor = serviceProvider.GetRequiredBackstageService<IBackstageToolsExecutor>();
        this._canIgnoreRecoverableExceptions = serviceProvider.GetRequiredBackstageService<IRecoverableExceptionService>().CanIgnore;
        this._standardDirectories = serviceProvider.GetRequiredBackstageService<IStandardDirectories>();
        this._randomNumberGenerator = serviceProvider.GetRequiredBackstageService<RandomNumberGenerator>();
    }

    public abstract void ShowToastNotification( ToastNotification notification );

    protected virtual ProcessStartInfo GetProcessStartInfoForUrl( string url, BrowserMode browserMode ) => new( url ) { UseShellExecute = true };

    public void OpenExternalWebPage( string url, BrowserMode browserMode )
    {
        try
        {
            this.Logger.Trace?.Log( $"Opening '{url}'." );

            this._processExecutor.Start( this.GetProcessStartInfoForUrl( url, browserMode ) );
        }
        catch ( Exception e )
        {
            try
            {
                this.Logger.Error?.Log( $"Cannot start the welcome web page: {e.Message}" );
            }
            catch when ( this._canIgnoreRecoverableExceptions ) { }

            if ( !this._canIgnoreRecoverableExceptions )
            {
                throw;
            }
        }
    }

    private static int GetFreePort()
    {
        // Create a new socket
        using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
        {
            // Bind to an IP address and port 0, which tells the OS to choose a free port
            socket.Bind( new IPEndPoint( IPAddress.Loopback, 0 ) );

            // Get the local endpoint of the socket and cast it to IPEndPoint
            var localEndPoint = (IPEndPoint) socket.LocalEndPoint!;

            // Return the assigned port number
            return localEndPoint.Port;
        }
    }

    public async Task OpenConfigurationWebPageAsync( string path )
    {
        var port = GetFreePort();

        // The server binds to loopback, which is reachable by every local user account, so the port is not what keeps
        // other local users out: the per-session token is. See SetupWebServerToken.
        var authenticationToken = SetupWebServerToken.GenerateToken( this._randomNumberGenerator );
        var tokenFilePath = SetupWebServerToken.WriteTokenFile( this._standardDirectories.TempDirectory, authenticationToken );

        try
        {
            using var webServerProcess =
                this._backstageToolExecutor.Start(
                    BackstageTool.Worker,
                    "web",
                    "--port",
                    port.ToString( CultureInfo.InvariantCulture ),
                    "--token-file",
                    tokenFilePath );

            // Wait until the server has started.
            var baseAddress = new Uri( $"http://localhost:{port}/" );

            // The readiness probe must carry the token too, otherwise it would be answered with 401 forever.
            var pingAddress = new Uri( baseAddress, $"ping?{SetupWebServerToken.QueryParameterName}={authenticationToken}" );

            // ReSharper disable once ShortLivedHttpClient
            var httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds( 1 ) };

            var stopwatch = Stopwatch.StartNew();

            this.Logger.Trace?.Log( "Waiting for the HTTP server." );

            while ( true )
            {
                try
                {
                    if ( webServerProcess.HasExited )
                    {
                        this.Logger.Error?.Log( "The server process has exited prematurely." );

                        return;
                    }

                    var response = await httpClient.GetAsync( pingAddress );

                    if ( response.IsSuccessStatusCode )
                    {
                        break;
                    }
                }
                catch ( TaskCanceledException )
                {
                    // This happens because of the timeout.
                }
                catch ( HttpRequestException e )
                {
                    this.Logger.Warning?.Log( e.Message );
                }

                if ( stopwatch.Elapsed.TotalSeconds > 30 )
                {
                    this.Logger.Error?.Log( $"Timeout while waiting for {baseAddress}." );

                    return;
                }
            }

            // Carry the token to the browser. The requested path may already have a query string (the exception report
            // page takes a report id), so the separator depends on what is already there. The server sets a session
            // cookie on the first request and redirects to the same page without the token, so the token does not stay
            // in the address bar or in the browsing history.
            var pageUri = new Uri( baseAddress, path );

            var separator = string.IsNullOrEmpty( pageUri.Query ) ? "?" : "&";

            var url = $"{pageUri}{separator}{SetupWebServerToken.QueryParameterName}={authenticationToken}";

            this.OpenExternalWebPage( url, BrowserMode.Application );
        }
        finally
        {
            // The server deletes the token file as soon as it has read it, so this only matters when the server never
            // got that far, in which case the token must not be left on disk.
            SetupWebServerToken.DeleteTokenFile( tokenFilePath, this.Logger );
        }
    }
}