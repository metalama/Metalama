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
/// Tests of <see cref="LinuxMachineIdProvider"/>. The provider reads the files of the operating system through
/// <see cref="IFileSystem"/>, so these tests also run on the Windows agents that run the continuous integration
/// build.
/// </summary>
public sealed class LinuxMachineIdProviderTests : TestsBase
{
    private const string _machineId = "9d2a6f3b1c8e4a70b5d3e2f1a4c6b8d0";
    private const string _dBusMachineId = "1f0e2d3c4b5a69788796a5b4c3d2e1f0";

    public LinuxMachineIdProviderTests( ITestOutputHelper logger ) : base( logger ) { }

    private LinuxMachineIdProvider CreateProvider() => new( this.ServiceProvider );

    /// <summary>
    /// Verifies that the identifier is the content of the file written by <c>systemd</c>.
    /// </summary>
    [Fact]
    public void MachineIdIsTheContentOfTheSystemdFile()
    {
        this.WriteFile( LinuxMachineIdProvider.MachineIdPath, _machineId );

        Assert.Equal( _machineId, this.CreateProvider().GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the end of line that follows the identifier in the file is not part of the identifier.
    /// </summary>
    /// <remarks>
    /// The license audit hashes the value, so an end of line kept in the value would make this machine a different
    /// device from the same machine read by an implementation that removes it.
    /// </remarks>
    [Fact]
    public void MachineIdExcludesTheEndOfLine()
    {
        this.WriteFile( LinuxMachineIdProvider.MachineIdPath, _machineId + "\n" );

        Assert.Equal( _machineId, this.CreateProvider().GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the file of D-Bus is read when the file of <c>systemd</c> does not exist, which is the case of a
    /// system that does not run <c>systemd</c>.
    /// </summary>
    [Fact]
    public void MachineIdIsTheContentOfTheDBusFileWhenTheSystemdFileDoesNotExist()
    {
        this.WriteFile( LinuxMachineIdProvider.DBusMachineIdPath, _dBusMachineId );

        Assert.Equal( _dBusMachineId, this.CreateProvider().GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the file of <c>systemd</c> has priority over the file of D-Bus when both exist.
    /// </summary>
    [Fact]
    public void MachineIdIsTheContentOfTheSystemdFileWhenBothFilesExist()
    {
        this.WriteFile( LinuxMachineIdProvider.MachineIdPath, _machineId );
        this.WriteFile( LinuxMachineIdProvider.DBusMachineIdPath, _dBusMachineId );

        Assert.Equal( _machineId, this.CreateProvider().GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the file of D-Bus is read when the file of <c>systemd</c> exists but is empty, which is the case
    /// of a system whose identifier has not been generated yet.
    /// </summary>
    [Fact]
    public void MachineIdIsTheContentOfTheDBusFileWhenTheSystemdFileIsEmpty()
    {
        this.WriteFile( LinuxMachineIdProvider.MachineIdPath, "" );
        this.WriteFile( LinuxMachineIdProvider.DBusMachineIdPath, _dBusMachineId );

        Assert.Equal( _dBusMachineId, this.CreateProvider().GetUncachedMachineId() );
    }

    /// <summary>
    /// Verifies that the machine name is reported when neither file exists.
    /// </summary>
    [Fact]
    public void MachineIdFallsBackToTheMachineNameWhenNoFileExists()
    {
        Assert.Equal( Environment.MachineName, this.CreateProvider().GetUncachedMachineId() );
    }

    /// <summary>
    /// Writes a file of the operating system to the file system of the test.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="content">The content of the file.</param>
    private void WriteFile( string path, string content )
    {
        this.FileSystem.CreateDirectory( GetDirectoryPath( path ) );
        this.FileSystem.WriteAllText( path, content );
    }

    /// <summary>
    /// Gets the directory of a path of the Linux file system. <see cref="System.IO.Path.GetDirectoryName(string)"/>
    /// is not used because it interprets the path according to the operating system that runs the test.
    /// </summary>
    /// <param name="path">The path of a file.</param>
    /// <returns>The path of the directory that contains the file.</returns>
    private static string GetDirectoryPath( string path ) => path.Substring( 0, path.LastIndexOf( '/' ) );
}
