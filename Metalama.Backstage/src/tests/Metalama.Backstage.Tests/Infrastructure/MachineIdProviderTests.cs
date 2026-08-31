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
/// Tests of <see cref="MachineIdProvider"/>, the base class that every operating system implementation shares. The
/// implementation under test reports a value that the test chooses, so that these tests do not depend on the
/// operating system that runs them.
/// </summary>
public sealed class MachineIdProviderTests : TestsBase
{
    public MachineIdProviderTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Verifies that the identifier is the value read by the implementation.
    /// </summary>
    [Fact]
    public void MachineIdIsTheValueReadByTheImplementation()
    {
        var provider = new StubMachineIdProvider( this.ServiceProvider, () => "the-machine-id" );

        Assert.Equal( "the-machine-id", provider.GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the surrounding white space of the value read by the implementation is removed.
    /// </summary>
    /// <remarks>
    /// The license audit hashes the value, so a trailing end of line would make the same machine two devices, one for
    /// each implementation that happens to keep it.
    /// </remarks>
    [Fact]
    public void MachineIdIsTrimmed()
    {
        var provider = new StubMachineIdProvider( this.ServiceProvider, () => "  the-machine-id\r\n" );

        Assert.Equal( "the-machine-id", provider.GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the machine name is reported when the operating system reports no identifier.
    /// </summary>
    [Theory]
    [InlineData( null )]
    [InlineData( "" )]
    [InlineData( "   " )]
    public void MachineIdFallsBackToTheMachineNameWhenTheImplementationReadsNothing( string? machineId )
    {
        var provider = new StubMachineIdProvider( this.ServiceProvider, () => machineId );

        Assert.Equal( Environment.MachineName, provider.GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the machine name is reported when the implementation throws.
    /// </summary>
    /// <remarks>
    /// The identifier is only reported by telemetry, so no failure to read it may prevent the product from working.
    /// </remarks>
    [Fact]
    public void MachineIdFallsBackToTheMachineNameWhenTheImplementationThrows()
    {
        var provider = new StubMachineIdProvider( this.ServiceProvider, () => throw new InvalidOperationException( "The test throws." ) );

        Assert.Equal( Environment.MachineName, provider.GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the identifier is read at most once in the process, and not once per instance of the provider.
    /// </summary>
    /// <remarks>
    /// Reading the identifier costs a registry access, a file read or a child process, and the value cannot change
    /// while the process runs. Several service providers can be built in one process, and each of them creates its
    /// own instance of the provider.
    /// </remarks>
    [Fact]
    public void MachineIdIsCachedForTheWholeProcess()
    {
        var firstProvider = new StubMachineIdProvider( this.ServiceProvider, () => "the-first-machine-id" );
        var secondProvider = new StubMachineIdProvider( this.ServiceProvider, () => "the-second-machine-id" );

        var firstMachineId = firstProvider.MachineId;

        Assert.Equal( firstMachineId, secondProvider.MachineId );
        Assert.Equal( 0, secondProvider.ReadCount );
    }

    /// <summary>
    /// An implementation of <see cref="MachineIdProvider"/> that reads the value the test gives it, and counts the
    /// reads.
    /// </summary>
    private sealed class StubMachineIdProvider : MachineIdProvider
    {
        private readonly Func<string?> _read;

        /// <summary>
        /// Gets the number of times the identifier has been read from this instance.
        /// </summary>
        public int ReadCount { get; private set; }

        public StubMachineIdProvider( IServiceProvider serviceProvider, Func<string?> read ) : base( serviceProvider )
        {
            this._read = read;
        }

        protected override string? ReadMachineId()
        {
            this.ReadCount++;

            return this._read();
        }
    }
}
