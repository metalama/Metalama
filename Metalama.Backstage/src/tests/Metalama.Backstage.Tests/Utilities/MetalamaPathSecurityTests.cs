// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using System;
using System.IO;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.Versioning;
#endif
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Utilities;

// Regression tests for https://github.com/metalama/Metalama/issues/1650.
// Metalama writes artifacts it later loads/executes under the temp directory. If that directory
// is writable by other (lower-privilege) users, those users can plant malicious DLLs that Metalama
// then loads. MetalamaPathUtilities must therefore be able to tell whether a directory is writable
// by users other than the current one.
public sealed class MetalamaPathSecurityTests
{
    private readonly ITestOutputHelper _logger;

    public MetalamaPathSecurityTests( ITestOutputHelper logger )
    {
        this._logger = logger;
    }

    [Fact]
    public void DirectoryWritableOnlyByCurrentUserIsNotFlagged()
    {
        var directory = CreateTempDirectory();

        try
        {
            RestrictToCurrentUser( directory );

            Assert.False(
                MetalamaPathUtilities.IsDirectoryWritableByOtherUsers( directory ),
                "A directory writable only by the current user must not be reported as writable by other users." );
        }
        finally
        {
            this.TryDeleteDirectory( directory );
        }
    }

    [Fact]
    public void DirectoryWritableByOtherUsersIsFlagged()
    {
        var directory = CreateTempDirectory();

        try
        {
            GrantWriteToOtherUsers( directory );

            Assert.True(
                MetalamaPathUtilities.IsDirectoryWritableByOtherUsers( directory ),
                "A directory writable by all users must be reported as such, because it allows DLL planting." );
        }
        finally
        {
            this.TryDeleteDirectory( directory );
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine( Path.GetTempPath(), "Metalama_Test_1650_" + Guid.NewGuid().ToString( "N" ) );
        Directory.CreateDirectory( directory );

        return directory;
    }

    private static void RestrictToCurrentUser( string directory )
    {
        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            RestrictToCurrentWindowsUser( directory );
        }
        else
        {
            SetUnixMode( directory, otherWritable: false );
        }
    }

    private static void GrantWriteToOtherUsers( string directory )
    {
        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            GrantWriteToEveryone( directory );
        }
        else
        {
            SetUnixMode( directory, otherWritable: true );
        }
    }

#if NET
    [SupportedOSPlatform( "windows" )]
#endif
    private static void RestrictToCurrentWindowsUser( string directory )
    {
        var directoryInfo = new DirectoryInfo( directory );
        var security = new DirectorySecurity();
        var currentUser = WindowsIdentity.GetCurrent().User!;

        // Disable inheritance (drop inherited rules) and grant full control to the current user only.
        security.SetAccessRuleProtection( true, false );
        security.SetOwner( currentUser );

        security.AddAccessRule(
            new FileSystemAccessRule(
                currentUser,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow ) );

        directoryInfo.SetAccessControl( security );
    }

#if NET
    [SupportedOSPlatform( "windows" )]
#endif
    private static void GrantWriteToEveryone( string directory )
    {
        var directoryInfo = new DirectoryInfo( directory );
        var security = directoryInfo.GetAccessControl();
        var everyone = new SecurityIdentifier( WellKnownSidType.WorldSid, null );

        security.AddAccessRule(
            new FileSystemAccessRule(
                everyone,
                FileSystemRights.Modify,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow ) );

        directoryInfo.SetAccessControl( security );
    }

    private static void SetUnixMode( string directory, bool otherWritable )
    {
#if NET
        var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

        if ( otherWritable )
        {
            mode |= UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                    | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;
        }

        File.SetUnixFileMode( directory, mode );
#else
        _ = directory;
        _ = otherWritable;

        throw new PlatformNotSupportedException();
#endif
    }

    private void TryDeleteDirectory( string directory )
    {
        try
        {
            Directory.Delete( directory, recursive: true );
        }
        catch ( Exception e )
        {
            this._logger.WriteLine( $"Failed to delete '{directory}': {e.Message}" );
        }
    }
}
