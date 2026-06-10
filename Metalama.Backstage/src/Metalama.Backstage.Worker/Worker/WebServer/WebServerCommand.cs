// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Services;
using Metalama.Backstage.Worker.Logger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Worker.WebServer;

[UsedImplicitly]
internal class WebServerCommand : AsyncCommand<WebServerCommandSettings>
{
    /// <summary>
    /// The set of <c>Host</c> header values accepted by the local setup server. The server only binds to the loopback
    /// interface, so only loopback host names and addresses are allowed.
    /// </summary>
    internal static IReadOnlyList<string> AllowedHosts { get; } = new[] { "localhost", "127.0.0.1", "[::1]" };

    /// <summary>
    /// The UTC tick count at which the server should shut down. It is written from the <c>ping</c> request-handler thread
    /// (via the keep-alive callback) and read from the command loop, so all accesses must go through <see cref="Volatile"/>
    /// to avoid a data race and torn reads.
    /// </summary>
    private long _shutDownTimeTicks;

    protected override async Task<int> ExecuteAsync( CommandContext context, WebServerCommandSettings settings, CancellationToken cancellationToken )
    {
        var appData = (AppData) context.Data!;

        var builder = WebApplication.CreateBuilder( new WebApplicationOptions() { ApplicationName = "Metalama.Backstage.Worker" } );

        builder.WebHost.ConfigureKestrel( serverOptions => serverOptions.ListenLocalhost( settings.Port ) );

        this.ExtendShutDownTime();

        var app = BuildWebApplication( builder, appData, this.ExtendShutDownTime );

        var serverTask = app.RunAsync();

        while ( true )
        {
            var shutDownTime = new DateTime( Volatile.Read( ref this._shutDownTimeTicks ), DateTimeKind.Utc );
            var now = DateTime.UtcNow;

            if ( shutDownTime <= now )
            {
                break;
            }

            if ( serverTask.IsCompleted )
            {
                // This would happen if the server cannot start.
                await serverTask;

                break;
            }

            await Task.Delay( shutDownTime - now, cancellationToken );
        }

        await app.StopAsync( cancellationToken );

        return 0;
    }

    /// <summary>
    /// Extends the server lifetime by one minute. Invoked when the <c>ping</c> endpoint is hit.
    /// </summary>
    private void ExtendShutDownTime() => Volatile.Write( ref this._shutDownTimeTicks, DateTime.UtcNow.AddMinutes( 1 ).Ticks );

    /// <summary>
    /// Configures the services and the request pipeline of the local setup web server and returns the built
    /// <see cref="WebApplication"/>. The caller is responsible for configuring the web host (e.g. Kestrel or a test
    /// server) before calling this method, and for running the returned application.
    /// </summary>
    /// <param name="onKeepAlive">Action invoked when the <c>ping</c> endpoint is hit, used to extend the server lifetime.</param>
    internal static WebApplication BuildWebApplication( WebApplicationBuilder builder, AppData appData, Action onKeepAlive )
    {
        builder.Services.AddControllers();
        builder.Services.AddRazorPages();

        // Restrict the 'Host' header to the loopback interface. The server only ever binds to localhost, so any request
        // carrying a different 'Host' header is either a misconfiguration or a DNS-rebinding attempt from a local website.
        builder.Services.AddHostFiltering( options => options.AllowedHosts = AllowedHosts.ToList() );

        builder.Services.Add(
            new ServiceDescriptor( typeof(ILoggerProvider), serviceProvider => new DotNetLoggerProvider( serviceProvider ), ServiceLifetime.Singleton ) );

        // Inject backstage services into the ASP.NET service collection.
        foreach ( var service in appData.ServiceCollection )
        {
            builder.Services.Add( service );
        }

        builder.Services.AddSingleton<RecaptchaService>();

        // Add services to the container.
        var app = builder.Build();

        app.Services.GetRequiredService<RecaptchaService>().Initialize();

        // Reject requests whose 'Host' header does not target the loopback interface. This must run before any other
        // middleware so that rejected requests never reach the application.
        app.UseHostFiltering();

        // If the program was started from the wrong directory, fix the path of static files.
        var contentRootPath = builder.Environment.ContentRootPath;

        if ( !Directory.Exists( Path.Combine( contentRootPath, "wwwroot" ) ) )
        {
            var binaryDirectory = Path.GetDirectoryName( typeof(WebServerCommand).Assembly.Location )!;
            contentRootPath = Path.Combine( binaryDirectory, "wwwroot" );
            app.UseStaticFiles( new StaticFileOptions() { FileProvider = new PhysicalFileProvider( contentRootPath ) } );
        }
        else
        {
            app.UseStaticFiles();
        }

        app.UseDeveloperExceptionPage();

        app.UseRouting();
        app.UseAuthorization();
        app.MapRazorPages();
        app.MapGet( "ping", onKeepAlive );

        return app;
    }
}
