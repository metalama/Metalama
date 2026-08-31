// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Infrastructure;

/// <summary>
/// Tests of <see cref="MachineIdProvider"/>, which reads the identifier of the machine that runs the test. The other
/// tests substitute this service (see <see cref="TestMachineIdProvider"/>), so this class is the only one that
/// exercises the real implementation.
/// </summary>
public sealed class MachineIdProviderTests : TestsBase
{
    public MachineIdProviderTests( ITestOutputHelper logger ) : base( logger ) { }

    private MachineIdProvider CreateProvider() => new( this.ServiceProvider );

    /// <summary>
    /// Verifies that the provider reports a non-empty identifier on every operating system. The license audit hashes
    /// this value, so an empty value would make every machine the same device.
    /// </summary>
    [Fact]
    public void MachineIdIsNotEmpty()
    {
        var machineId = this.CreateProvider().MachineId;
        this.Logger.WriteLine( machineId );

        Assert.False( string.IsNullOrWhiteSpace( machineId ) );
    }

    /// <summary>
    /// Verifies that the identifier does not change between two reads, and that two instances of the provider report
    /// the same value.
    /// </summary>
    /// <remarks>
    /// The license audit counts the devices of one user over a period longer than a single process, so an identifier
    /// that changes between two processes would count one machine several times. See issue #1873.
    /// </remarks>
    [Fact]
    public void MachineIdIsStable()
    {
        var provider = this.CreateProvider();

        Assert.Equal( provider.MachineId, provider.MachineId );
        Assert.Equal( provider.MachineId, this.CreateProvider().MachineId );
    }

    /// <summary>
    /// Verifies that the identifier reported on Windows is the <c>MachineGuid</c> value of the 32-bit view of the
    /// registry, which is the value that PostSharp reads.
    /// </summary>
    /// <remarks>
    /// The key is subject to registry redirection, so the 32-bit view and the 64-bit view can hold different values
    /// on the same machine, and only the 32-bit view is comparable with the values that PostSharp reports. See issue
    /// #1873.
    /// </remarks>
    [Fact]
    public void MachineIdIsTheMachineGuidOfTheThirtyTwoBitRegistryViewOnWindows()
    {
        if ( !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            return;
        }

#pragma warning disable CA1416 // The code is guarded by a platform check.
        using var hive = RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Registry32 );
        using var key = hive.OpenSubKey( @"SOFTWARE\Microsoft\Cryptography" );
        var expected = key?.GetValue( "MachineGuid" ) as string;
#pragma warning restore CA1416

        if ( expected == null )
        {
            // The value is created by the operating system at installation time, so it is expected to be present.
            // A machine that does not have it exercises the fallback instead, which the assertion below covers.
            Assert.Equal( Environment.MachineName, this.CreateProvider().MachineId );

            return;
        }

        Assert.Equal( expected, this.CreateProvider().MachineId );
    }
}
