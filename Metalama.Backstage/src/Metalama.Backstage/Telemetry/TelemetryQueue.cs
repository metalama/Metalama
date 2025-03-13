// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;
using System.IO;

namespace Metalama.Backstage.Telemetry
{
    internal sealed class TelemetryQueue
    {
        private readonly IStandardDirectories _directories;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public TelemetryQueue( IServiceProvider serviceProvider )
        {
            this._directories = serviceProvider.GetRequiredBackstageService<IStandardDirectories>();
            this._logger = serviceProvider.GetLoggerFactory().Telemetry();
            this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        }

        public void EnqueueFile( string file )
        {
            this._logger.Trace?.Log( $"Enqueuing '{file}'." );

            var directory = this._directories.TelemetryUploadQueueDirectory;

            if ( !this._fileSystem.DirectoryExists( directory ) )
            {
                this._fileSystem.CreateDirectory( directory );
            }

            this._fileSystem.MoveFile( file, Path.Combine( directory, Path.GetFileName( file ) ) );
        }
    }
}