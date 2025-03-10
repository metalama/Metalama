// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Metalama.Backstage.Utilities;

internal sealed class ParentProcessSearchLinux : ParentProcessSearchBase<int>
{
    public ParentProcessSearchLinux( ILogger logger ) : base( logger ) { }

    protected override bool IsNull( int handle ) => handle == 0;

    protected override int GetCurrentProcessHandle() => Process.GetCurrentProcess().Id;

    protected override (string? ImageName, int CurrentProcessId, int ParentProcessHandle) GetProcessInfo( int processHandle )
    {
        // There's no handle on Linux.
        var processId = processHandle;
        string? commandImageName;

        try
        {
            commandImageName = File.ReadAllText( "/proc/" + processId + "/comm" ).Trim();
        }
        catch ( Exception e )
        {
            this.Logger.Error?.Log( $"Could not read '/proc/{processId}/comm' file: {e.Message}" );
            commandImageName = null;
        }

        // Read status file of the process.
        string? processStatus;

        try
        {
            processStatus = File.ReadAllText( "/proc/" + processId + "/stat" );
        }
        catch ( Exception e )
        {
            this.Logger.Error?.Log( $"Could not read '/proc/{processId}/stat' file: {e.Message}" );

            throw;
        }

        var processStatusArray = processStatus.Split( ' ' );
        int parentProcessId;

        // Try parse PPID from 4th value of status information, then add the process to list of processes.
        try
        {
            parentProcessId = int.Parse( processStatusArray[3], CultureInfo.InvariantCulture );
        }
        catch ( Exception e )
        {
            this.Logger.Error?.Log( $"Could not parse PPID from process '{processId}' status file: {e.Message}" );

            throw;
        }

        return (commandImageName, processId, parentProcessId);
    }

    protected override void CloseProcessHandle( int handle ) { }
}