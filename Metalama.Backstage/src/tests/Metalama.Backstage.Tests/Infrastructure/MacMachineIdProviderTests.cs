// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Infrastructure;

/// <summary>
/// Tests of <see cref="MacMachineIdProvider"/>. The provider runs the <c>ioreg</c> command through
/// <see cref="IProcessExecutor"/>, so these tests also run on the Windows agents that run the continuous integration
/// build.
/// </summary>
public sealed class MacMachineIdProviderTests : TestsBase
{
    private const string _platformUuid = "3A1B5C7D-9E2F-4A6B-8C0D-1E3F5A7B9C2D";

    /// <summary>
    /// An extract of the output of the <c>ioreg</c> command, shortened to the properties that matter here.
    /// </summary>
    private const string _ioregOutput = @"
+-o MacBookPro18,3  <class IOPlatformExpertDevice, id 0x100000253, registered, matched, active, busy 0 (2 ms)>
  {
    ""IOPlatformSerialNumber"" = ""C02ABCDEFGHI""
    ""IOPlatformUUID"" = ""3A1B5C7D-9E2F-4A6B-8C0D-1E3F5A7B9C2D""
    ""IOBusyInterest"" = ""IOCommand is not serializable""
  }
";

    public MacMachineIdProviderTests( ITestOutputHelper logger ) : base( logger ) { }

    private MacMachineIdProvider CreateProvider() => new( this.ServiceProvider );

    /// <summary>
    /// Verifies that the identifier is the <c>IOPlatformUUID</c> property reported by the <c>ioreg</c> command.
    /// </summary>
    [Fact]
    public void MachineIdIsThePlatformUuid()
    {
        this.ProcessExecutor.StandardOutputProvider = _ => _ioregOutput;

        Assert.Equal( _platformUuid, this.CreateProvider().GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the identifier is read from the platform expert device of the input and output registry.
    /// </summary>
    [Fact]
    public void MachineIdIsReadFromThePlatformExpertDevice()
    {
        this.ProcessExecutor.StandardOutputProvider = _ => _ioregOutput;

        _ = this.CreateProvider().GetUncachedMachineId();

        var startInfo = Assert.Single( this.ProcessExecutor.StartedProcesses );
        Assert.Equal( "ioreg", startInfo.FileName );
        Assert.Contains( "IOPlatformExpertDevice", startInfo.Arguments, StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that the machine name is reported when the command does not complete successfully.
    /// </summary>
    [Fact]
    public void MachineIdFallsBackToTheMachineNameWhenTheCommandFails()
    {
        this.ProcessExecutor.StandardOutputProvider = _ => null;

        Assert.Equal( Environment.MachineName, this.CreateProvider().GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the machine name is reported when the command cannot be started, which is what happens when the
    /// executable is not present.
    /// </summary>
    [Fact]
    public void MachineIdFallsBackToTheMachineNameWhenTheCommandCannotBeStarted()
    {
        this.ProcessExecutor.ExceptionToThrow = new InvalidOperationException( "The command cannot be started." );

        Assert.Equal( Environment.MachineName, this.CreateProvider().GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the machine name is reported when the output of the command has no <c>IOPlatformUUID</c>
    /// property.
    /// </summary>
    [Fact]
    public void MachineIdFallsBackToTheMachineNameWhenTheOutputHasNoPlatformUuid()
    {
        this.ProcessExecutor.StandardOutputProvider = _ => "+-o Root  <class IORegistryEntry, id 0x100000100, retain 15>";

        Assert.Equal( Environment.MachineName, this.CreateProvider().GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the machine name is reported when the <c>IOPlatformUUID</c> property is empty.
    /// </summary>
    [Fact]
    public void MachineIdFallsBackToTheMachineNameWhenThePlatformUuidIsEmpty()
    {
        this.ProcessExecutor.StandardOutputProvider = _ => @"    ""IOPlatformUUID"" = """"";

        Assert.Equal( Environment.MachineName, this.CreateProvider().GetUncachedMachineId() );
    }
}
