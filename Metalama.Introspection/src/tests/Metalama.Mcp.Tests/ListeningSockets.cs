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
/// Lists the listening TCP sockets and the bound UDP sockets, with their owning process and whether they are bound to a
/// loopback address only.
/// </summary>
internal static class ListeningSockets
{
    internal sealed record Listener( int ProcessId, string LocalAddress, bool IsLoopback );

    public static IReadOnlyList<Listener> GetListeners() => OperatingSystem.IsWindows() ? GetOnWindows() : GetOnLinux();

    public static ISet<int> GetOwningProcessIds() => GetListeners().Select( l => l.ProcessId ).ToHashSet();

    private static List<Listener> GetOnWindows()
    {
        var result = new List<Listener>();

        foreach ( var protocol in new[] { "TCP", "TCPv6", "UDP", "UDPv6" } )
        {
            using var process = Process.Start( new ProcessStartInfo( "netstat", $"-ano -p {protocol}" ) { RedirectStandardOutput = true } )!;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // The lines end with \r\n. Before this was trimmed, no process identifier parsed, and the check that nothing
            // listens passed whatever the processes did.
            foreach ( var line in output.Split( '\n', StringSplitOptions.TrimEntries ) )
            {
                var columns = line.Split( ' ', StringSplitOptions.RemoveEmptyEntries );

                // TCP rows: proto, local, foreign, state, pid. UDP rows have no state, and every UDP socket is bound.
                var isListening = protocol.StartsWith( "UDP", StringComparison.Ordinal )
                    ? columns.Length == 4
                    : columns.Length == 5 && columns[3] == "LISTENING";

                if ( isListening && int.TryParse( columns[^1], NumberStyles.None, CultureInfo.InvariantCulture, out var pid ) )
                {
                    var address = columns[1];
                    var isLoopback = address.StartsWith( "127.", StringComparison.Ordinal ) || address.StartsWith( "[::1]", StringComparison.Ordinal );
                    result.Add( new Listener( pid, address, isLoopback ) );
                }
            }
        }

        return result;
    }

    private static List<Listener> GetOnLinux()
    {
        // The local addresses of listening TCP sockets (state 0A) and of all UDP sockets, by inode.
        var addresses = new Dictionary<string, (string Address, bool IsLoopback)>();

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
                    // The address is hexadecimal: 0100007F is 127.0.0.1, and the IPv6 loopback ends with 01000000.
                    var address = columns[1].Split( ':' )[0];
                    var isLoopback = address.EndsWith( "7F", StringComparison.Ordinal ) || address == "00000000000000000000000001000000";
                    addresses[columns[9]] = (columns[1], isLoopback);
                }
            }
        }

        var result = new List<Listener>();

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

                    if ( target != null && target.StartsWith( "socket:[", StringComparison.Ordinal ) && addresses.TryGetValue( target[8..^1], out var socket ) )
                    {
                        result.Add( new Listener( pid, socket.Address, socket.IsLoopback ) );
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
