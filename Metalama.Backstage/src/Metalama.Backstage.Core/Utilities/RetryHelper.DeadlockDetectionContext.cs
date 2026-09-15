// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Backstage.Utilities;

public static partial class RetryHelper
{
    private class DeadlockDetectionContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger? _logger;
        private readonly IReadOnlyList<string> _files;

        public DeadlockDetectionContext( IServiceProvider serviceProvider, ILogger? logger, IReadOnlyList<string> files )
        {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
            this._files = files;
        }

        public void OnRecoverableException( Exception exception )
        {
            var lockingDetection = this._serviceProvider.GetBackstageService<ILockingProcessDetector>();

            if ( lockingDetection != null && this._logger != null )
            {
                var lockingProcesses = lockingDetection.GetProcessesUsingFiles( this._files );

                if ( lockingProcesses.Count == 0 )
                {
                    this._logger.Trace?.Log( "No process locking these files was found." );
                }
                else
                {
                    this._logger.Warning?.Log(
                        "The following process(es) are locking these files: " + string.Join(
                            ", ",
                            lockingProcesses.Select( p => $"{p.ProcessName} ({p.Id})" ) ) );
                }
            }
        }

        public void OnFatalException( Exception e )
        {
            var lockingDetection = this._serviceProvider.GetBackstageService<ILockingProcessDetector>();

            if ( lockingDetection != null )
            {
                var lockingProcesses = lockingDetection.GetProcessesUsingFiles( this._files );

                if ( lockingProcesses.Count > 0 )
                {
                    var additionalMessage =
                        $" The following process(es) are locking the file(s): {string.Join( ", ", lockingProcesses.Select( p => $"{p.ProcessName} ({p.Id})" ) )}.";

                    throw new LockedFileException( e.Message + additionalMessage, e );
                }
            }
        }
    }
}