// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Win32;
using System;

namespace Metalama.Backstage.Infrastructure;

/// <summary>
/// Reads the identifier of the machine on Windows.
/// </summary>
/// <remarks>
/// The value is the <c>MachineGuid</c> value of the <c>SOFTWARE\Microsoft\Cryptography</c> key of the local machine
/// hive, read from the 32-bit view of the registry. This is the value that PostSharp reads, and the 32-bit view is
/// part of the specification: the key is subject to registry redirection, so the 32-bit view and the 64-bit view can
/// hold different values on the same machine, and only the 32-bit value is comparable with the values that PostSharp
/// reports. See issue #1873.
/// </remarks>
internal sealed class WindowsMachineIdProvider : MachineIdProvider
{
    private const string _registryKeyName = @"SOFTWARE\Microsoft\Cryptography";
    private const string _registryValueName = "MachineGuid";

    public WindowsMachineIdProvider( IServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override string? ReadMachineId()
    {
#pragma warning disable CA1416 // This class is only instantiated on Windows.
        using var hive = RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Registry32 );
        using var key = hive.OpenSubKey( _registryKeyName );

        return key?.GetValue( _registryValueName ) as string;
#pragma warning restore CA1416
    }
}
