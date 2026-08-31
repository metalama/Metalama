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
/// Tests of <see cref="WindowsMachineIdProvider"/>, which reads the registry of the machine that runs the test. These
/// tests are therefore skipped on the other operating systems.
/// </summary>
public sealed class WindowsMachineIdProviderTests : TestsBase
{
    private const string _skipReason = "The registry is only read on Windows.";

    /// <summary>
    /// Gets a value indicating whether the test runs on Windows. The type is qualified because <c>TestsBase</c>
    /// exposes a property of the same name, which reports the platform simulated by the test.
    /// </summary>
    private static bool IsWindows => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform( OSPlatform.Windows );

    public WindowsMachineIdProviderTests( ITestOutputHelper logger ) : base( logger ) { }

    private WindowsMachineIdProvider CreateProvider() => new( this.ServiceProvider );

    /// <summary>
    /// Verifies that the provider reports a non-empty identifier. The license audit hashes this value, so an empty
    /// value would make every machine the same device.
    /// </summary>
    [SkippableFact]
    public void MachineIdIsNotEmpty()
    {
        Skip.IfNot( IsWindows, _skipReason );

        var machineId = this.CreateProvider().GetUncachedMachineId();
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
    [SkippableFact]
    public void MachineIdIsStable()
    {
        Skip.IfNot( IsWindows, _skipReason );

        var provider = this.CreateProvider();

        Assert.Equal( provider.GetUncachedMachineId(), provider.GetUncachedMachineId() );
        Assert.Equal( provider.GetUncachedMachineId(), this.CreateProvider().GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the identifier is the <c>MachineGuid</c> value of the 32-bit view of the registry, which is the
    /// value that PostSharp reads.
    /// </summary>
    /// <remarks>
    /// The key is subject to registry redirection, so the 32-bit view and the 64-bit view can hold different values
    /// on the same machine, and only the 32-bit view is comparable with the values that PostSharp reports. See issue
    /// #1873.
    /// </remarks>
    [SkippableFact]
    public void MachineIdIsTheMachineGuidOfTheThirtyTwoBitRegistryView()
    {
        Skip.IfNot( IsWindows, _skipReason );

#pragma warning disable CA1416 // The code is guarded by a platform check.
        using var hive = RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Registry32 );
        using var key = hive.OpenSubKey( @"SOFTWARE\Microsoft\Cryptography" );
        var expected = key?.GetValue( "MachineGuid" ) as string;
#pragma warning restore CA1416

        // The value is created by the operating system when it is installed, so it is expected to be present. A
        // machine that does not have it exercises the fallback to the machine name instead.
        Assert.Equal( expected?.Trim() ?? Environment.MachineName, this.CreateProvider().GetUncachedMachineId() );
    }
}
