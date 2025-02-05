// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
using System.Threading.Tasks;

namespace Metalama.Backstage.Worker.WebServer;

[UsedImplicitly]
internal class WebServerCommand : AsyncCommand<WebServerCommandSettings>
{
    public override async Task<int> ExecuteAsync( CommandContext context, WebServerCommandSettings settings )
    {
        var appData = (AppData) context.Data!;

        var builder = WebApplication.CreateBuilder( new WebApplicationOptions() { ApplicationName = "Metalama.Backstage.Worker" } );

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

        builder.WebHost.ConfigureKestrel( serverOptions => serverOptions.ListenLocalhost( settings.Port ) );

        // Add services to the container.
        var app = builder.Build();
        
        app.Services.GetRequiredService<RecaptchaService>().Initialize();

        app.UseCors();

        // If the program was started from the wrong directory, fix the path of static files.
        var contentRootPath = builder.Environment.ContentRootPath;

        if ( !Directory.Exists( Path.Combine( contentRootPath, "wwwroot" ) ) )
        {
            var binaryDirectory = Path.GetDirectoryName( this.GetType().Assembly.Location )!;
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
        app.MapGet( "ping", KeepAlive );

        var serverTask = app.RunAsync();
        var shutDownTime = DateTime.UtcNow.AddMinutes( 1 );

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
                await Task.Delay( delay );
            }
        }

        await app.StopAsync();

        return 0;

        void KeepAlive()
        {
            shutDownTime = DateTime.UtcNow.AddMinutes( 1 );
        }
    }
}