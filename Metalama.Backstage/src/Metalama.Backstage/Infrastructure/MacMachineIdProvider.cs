// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Metalama.Backstage.Infrastructure;

/// <summary>
/// Reads the identifier of the machine on macOS.
/// </summary>
/// <remarks>
/// macOS has no PostSharp implementation to be compatible with, so this class reads the identifier that the operating
/// system itself considers stable: the <c>IOPlatformUUID</c> property of the platform expert device, which the
/// <c>ioreg</c> command reports. See issue #1873.
/// </remarks>
internal sealed class MacMachineIdProvider : MachineIdProvider
{
    private const string _commandName = "ioreg";
    private const string _commandArguments = "-rd1 -c IOPlatformExpertDevice";

    private static readonly TimeSpan _commandTimeout = TimeSpan.FromSeconds( 10 );

    private static readonly Regex _platformUuidRegex = new( @"""IOPlatformUUID""\s*=\s*""(?<uuid>[^""]*)""" );

    private readonly IProcessExecutor _processExecutor;

    public MacMachineIdProvider( IServiceProvider serviceProvider ) : base( serviceProvider )
    {
        this._processExecutor = serviceProvider.GetRequiredBackstageService<IProcessExecutor>();
    }

    protected override string? ReadMachineId()
    {
        var startInfo = new ProcessStartInfo( _commandName, _commandArguments );

        if ( !this._processExecutor.TryReadStandardOutput( startInfo, _commandTimeout, out var output ) )
        {
            this.Logger.Warning?.Log( $"The '{_commandName}' command did not complete successfully." );

            return null;
        }

        var match = _platformUuidRegex.Match( output );

        if ( !match.Success )
        {
            this.Logger.Warning?.Log( $"The '{_commandName}' command reported no IOPlatformUUID." );

            return null;
        }

        return match.Groups["uuid"].Value;
    }
}
