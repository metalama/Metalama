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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Worker.WebServer;

[UsedImplicitly]
internal class WebServerCommand : AsyncCommand<WebServerCommandSettings>
{
    protected override async Task<int> ExecuteAsync( CommandContext context, WebServerCommandSettings settings, CancellationToken cancellationToken )
    {
        var appData = (AppData) context.Data!;

        var builder = WebApplication.CreateBuilder( new WebApplicationOptions() { ApplicationName = "Metalama.Backstage.Worker" } );

        builder.WebHost.ConfigureKestrel( serverOptions => serverOptions.ListenLocalhost( settings.Port ) );

        var shutDownTime = DateTime.UtcNow.AddMinutes( 1 );

        var app = BuildWebApplication( builder, appData, () => shutDownTime = DateTime.UtcNow.AddMinutes( 1 ) );

        var serverTask = app.RunAsync();

        while ( shutDownTime > DateTime.UtcNow )
        {
            if ( serverTask.IsCompleted )
            {
                // This would happen if the server cannot start.
                await serverTask;

                break;
            }

            var delay = shutDownTime - DateTime.UtcNow;

            if ( delay > TimeSpan.Zero )
            {
                await Task.Delay( delay, cancellationToken );
            }
        }

        await app.StopAsync( cancellationToken );

        return 0;
    }

    /// <summary>
    /// Configures the services and the request pipeline of the local setup web server and returns the built
    /// <see cref="WebApplication"/>. The caller is responsible for configuring the web host (e.g. Kestrel or a test
    /// server) before calling this method, and for running the returned application.
    /// </summary>
    /// <param name="onKeepAlive">Action invoked when the <c>ping</c> endpoint is hit, used to extend the server lifetime.</param>
    internal static WebApplication BuildWebApplication( WebApplicationBuilder builder, AppData appData, Action onKeepAlive )
    {
        builder.Services.AddCors();
        builder.Services.AddControllers();
        builder.Services.AddRazorPages();

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

        app.UseCors();

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
