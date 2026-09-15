// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System;
using System.Linq;

namespace Metalama.Backstage.Infrastructure;

/// <summary>
/// Reads the identifier of the machine on Linux.
/// </summary>
/// <remarks>
/// Linux has no PostSharp implementation to be compatible with, so this class reads the identifier that the operating
/// system itself considers stable: <c>/etc/machine-id</c>, which <c>systemd</c> generates when the system is
/// installed, and then <c>/var/lib/dbus/machine-id</c>, which is where D-Bus keeps the same value on a system that
/// has no <c>systemd</c>. See issue #1873.
/// </remarks>
internal sealed class LinuxMachineIdProvider : MachineIdProvider
{
    internal const string MachineIdPath = "/etc/machine-id";
    internal const string DBusMachineIdPath = "/var/lib/dbus/machine-id";

    private readonly IFileSystem _fileSystem;

    public LinuxMachineIdProvider( IServiceProvider serviceProvider ) : base( serviceProvider )
    {
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
    }

    protected override string? ReadMachineId() => this.ReadFirstLine( MachineIdPath ) ?? this.ReadFirstLine( DBusMachineIdPath );

    private string? ReadFirstLine( string path )
    {
        if ( !this._fileSystem.FileExists( path ) )
        {
            return null;
        }

        return this._fileSystem.ReadAllLines( path ).FirstOrDefault( line => !string.IsNullOrWhiteSpace( line ) )?.Trim();
    }
}
