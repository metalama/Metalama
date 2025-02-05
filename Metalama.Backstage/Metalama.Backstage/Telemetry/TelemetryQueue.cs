// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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