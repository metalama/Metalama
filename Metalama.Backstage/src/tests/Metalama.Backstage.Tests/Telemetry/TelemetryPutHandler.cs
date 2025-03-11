// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NET
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Tests.Telemetry;

// This is a copy of the code from BusinessSystems\web\Services\SharpCrafters.Internal.Services.TelemetryReceiver.Program.
// Keep the code synchronized.
public sealed class TelemetryPutHandler
{
    private readonly IFileSystem _fileSystem;
    private readonly string _outputDirectory;

    public TelemetryPutHandler( IServiceProvider serviceProvider, string outputDirectory )
    {
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        this._outputDirectory = outputDirectory;
    }

    public async Task<IResult> HandleAsync( HttpRequest request, CancellationToken cancellationToken )
    {
        if ( request.Method != HttpMethod.Put.Method )
        {
            return Results.BadRequest();
        }

        var files = request.Form.Files;

        if ( files.Count != 1 )
        {
            return Results.BadRequest();
        }

        var file = files[0];

        if ( Path.GetExtension( file.FileName ) != ".psf" )
        {
            return Results.BadRequest();
        }

        var check = request.Query["check"];

        if ( check.Count != 1 )
        {
            return Results.BadRequest();
        }

        var expectedCheck = TelemetryUploader.ComputeHash( file.FileName );

        if ( expectedCheck != check )
        {
            return Results.BadRequest();
        }

        // ReSharper disable once UseAwaitUsing
        using ( var outputFile = this._fileSystem.Open(
                   Path.Combine( this._outputDirectory, file.FileName ),
                   FileMode.Create,
                   FileAccess.Write,
                   FileShare.None,
                   4096,
                   FileOptions.Asynchronous ) )
        {
            await file.CopyToAsync( outputFile, cancellationToken );
        }

        return Results.Ok();
    }
}

#endif