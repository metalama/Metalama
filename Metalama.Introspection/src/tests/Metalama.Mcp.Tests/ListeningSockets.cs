// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Metalama.Mcp.Tests;

/// <summary>
/// Lists the processes that own a listening TCP socket or a bound UDP socket.
/// </summary>
internal static class ListeningSockets
{
    public static ISet<int> GetOwningProcessIds() => OperatingSystem.IsWindows() ? GetOnWindows() : GetOnLinux();

    private static HashSet<int> GetOnWindows()
    {
        var result = new HashSet<int>();

        foreach ( var protocol in new[] { "TCP", "TCPv6", "UDP", "UDPv6" } )
        {
            using var process = Process.Start( new ProcessStartInfo( "netstat", $"-ano -p {protocol}" ) { RedirectStandardOutput = true } )!;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            foreach ( var line in output.Split( '\n' ) )
            {
                var columns = line.Split( ' ', StringSplitOptions.RemoveEmptyEntries );

                // TCP rows: proto, local, foreign, state, pid. UDP rows have no state, and every UDP socket is bound.
                var isListening = protocol.StartsWith( "UDP", StringComparison.Ordinal )
                    ? columns.Length == 4
                    : columns.Length == 5 && columns[3] == "LISTENING";

                if ( isListening && int.TryParse( columns[^1], NumberStyles.None, CultureInfo.InvariantCulture, out var pid ) )
                {
                    result.Add( pid );
                }
            }
        }

        return result;
    }

    private static HashSet<int> GetOnLinux()
    {
        // The inodes of listening TCP sockets (state 0A) and of all UDP sockets.
        var inodes = new HashSet<string>();

        foreach ( var file in new[] { "tcp", "tcp6", "udp", "udp6" } )
        {
            var path = "/proc/net/" + file;

            if ( !File.Exists( path ) )
            {
                continue;
            }

            foreach ( var line in File.ReadLines( path ).Skip( 1 ) )
            {
                var columns = line.Split( ' ', StringSplitOptions.RemoveEmptyEntries );

                if ( columns.Length > 9 && (file.StartsWith( "udp", StringComparison.Ordinal ) || columns[3] == "0A") )
                {
                    inodes.Add( columns[9] );
                }
            }
        }

        var result = new HashSet<int>();

        foreach ( var directory in Directory.EnumerateDirectories( "/proc" ) )
        {
            if ( !int.TryParse( Path.GetFileName( directory ), NumberStyles.None, CultureInfo.InvariantCulture, out var pid ) )
            {
                continue;
            }

            try
            {
                foreach ( var fd in Directory.EnumerateFileSystemEntries( Path.Combine( directory, "fd" ) ) )
                {
                    var target = new FileInfo( fd ).LinkTarget;

                    if ( target != null && target.StartsWith( "socket:[", StringComparison.Ordinal ) && inodes.Contains( target[8..^1] ) )
                    {
                        result.Add( pid );
                    }
                }
            }
            catch ( Exception e ) when ( e is UnauthorizedAccessException or IOException )
            {
                // Processes of other users.
            }
        }

        return result;
    }
}
