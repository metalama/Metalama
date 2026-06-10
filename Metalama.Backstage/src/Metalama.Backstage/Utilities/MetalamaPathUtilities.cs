// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// There is a copy of this code in Metalama.Compiler.Shared and partially in Metalama ResourceExtractor.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
#if NET
using System.Runtime.Versioning;
#else
using System.Globalization;
#endif

namespace Metalama.Backstage.Utilities;

[PublicAPI]
public static class MetalamaPathUtilities
{
    private static readonly string? _overriddenTempPath;
    private static readonly object _tempPathVerificationSync = new();
    private static volatile bool _tempPathVerified;

    static MetalamaPathUtilities()
    {
        var overriddenTempPath = Environment.GetEnvironmentVariable( "METALAMA_TEMP" );
        _overriddenTempPath = string.IsNullOrEmpty( overriddenTempPath ) ? null : overriddenTempPath;
    }

    public static string GetTempPath()
    {
        var tempPath = _overriddenTempPath ?? Path.GetTempPath();

        EnsureTempPathIsSecure( tempPath );

        return tempPath;
    }

    public static string GetTempFileName()
    {
        if ( _overriddenTempPath == null )
        {
            return Path.GetTempFileName();
        }

        // https://stackoverflow.com/a/10152460/4100001
        var attempt = 0;

        while ( true )
        {
            var path = Path.Combine( _overriddenTempPath, $"{Guid.NewGuid()}.tmp" );

            try
            {
                using ( var newFile = new FileStream( path, FileMode.Create ) )
                {
                    newFile.Close();
                }
            }
            catch ( IOException ) when ( ++attempt < 10 ) { continue; }

            return path;
        }
    }

    /// <summary>
    /// Ensures, only once per process, that the Metalama temporary directory is not writable by other users of the machine.
    /// Metalama loads and executes assemblies from this directory (compile-time assemblies, extracted tools, bootstrap dependencies),
    /// so a directory writable by lower-privilege users would let them plant code that Metalama then runs. See issue #1650.
    /// The directory is created (if necessary) and, when it is found to be writable by other users — typically because it was
    /// created under a world-writable root such as <c>/tmp</c> or <c>C:\Temp</c> and inherited permissive permissions —
    /// its permissions are tightened to the current user. A <see cref="SecurityException" /> is thrown only if the directory
    /// remains writable by other users after that attempt, e.g. because it is owned by another user.
    /// </summary>
    private static void EnsureTempPathIsSecure( string tempPath )
    {
        if ( _tempPathVerified )
        {
            return;
        }

        lock ( _tempPathVerificationSync )
        {
            if ( _tempPathVerified )
            {
                return;
            }

            var metalamaTempDirectory = Path.Combine( tempPath, "Metalama" );

            try
            {
                if ( !Directory.Exists( metalamaTempDirectory ) )
                {
                    Directory.CreateDirectory( metalamaTempDirectory );
                }

                // Several Metalama processes (e.g. concurrent compiler invocations) may secure the directory at the same time,
                // so we retry a few times before giving up, to tolerate a transient failure caused by such a race.
                const int maxAttempts = 4;

                for ( var attempt = 0; IsDirectoryWritableByOtherUsers( metalamaTempDirectory ); attempt++ )
                {
                    if ( attempt >= maxAttempts )
                    {
                        throw new SecurityException(
                            $"The Metalama temporary directory '{metalamaTempDirectory}' is writable by other users of this machine "
                            + "and its permissions could not be tightened automatically. "
                            + "This would allow them to plant assemblies that Metalama loads and executes. "
                            + "Restrict the permissions of this directory to the current user, or set the METALAMA_TEMP environment variable "
                            + "to a directory that only the current user can write to." );
                    }

                    TryRestrictDirectoryToCurrentUser( metalamaTempDirectory );
                }
            }
            catch ( SecurityException )
            {
                throw;
            }
            catch ( Exception )
            {
                // If we cannot determine or set the permissions (e.g. an unexpected platform or API failure), do not block the build.
            }

            _tempPathVerified = true;
        }
    }

    /// <summary>
    /// Best-effort tightening of the permissions of a directory so that only the current user (plus SYSTEM and Administrators on
    /// Windows) can write to it. Any failure is swallowed; the caller re-checks the result.
    /// </summary>
    private static void TryRestrictDirectoryToCurrentUser( string directory )
    {
        try
        {
            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
            {
                RestrictDirectoryToCurrentUserOnWindows( directory );
            }
            else
            {
                RestrictDirectoryToOwnerOnUnix( directory );
            }
        }
        catch ( Exception )
        {
            // Best effort.
        }
    }

#if NET
    [SupportedOSPlatform( "windows" )]
#endif
    private static void RestrictDirectoryToCurrentUserOnWindows( string directory )
    {
        // Replace the whole ACL by a protected (non-inherited) one that grants full control only to the current user,
        // SYSTEM and the Administrators group.
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection( isProtected: true, preserveInheritance: false );

        const InheritanceFlags inheritanceFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

        using ( var identity = WindowsIdentity.GetCurrent() )
        {
            if ( identity.User != null )
            {
                security.AddAccessRule(
                    new FileSystemAccessRule( identity.User, FileSystemRights.FullControl, inheritanceFlags, PropagationFlags.None, AccessControlType.Allow ) );
            }
        }

        security.AddAccessRule(
            new FileSystemAccessRule(
                new SecurityIdentifier( WellKnownSidType.LocalSystemSid, null ),
                FileSystemRights.FullControl,
                inheritanceFlags,
                PropagationFlags.None,
                AccessControlType.Allow ) );

        security.AddAccessRule(
            new FileSystemAccessRule(
                new SecurityIdentifier( WellKnownSidType.BuiltinAdministratorsSid, null ),
                FileSystemRights.FullControl,
                inheritanceFlags,
                PropagationFlags.None,
                AccessControlType.Allow ) );

        new DirectoryInfo( directory ).SetAccessControl( security );
    }

#if NET
    [UnsupportedOSPlatform( "windows" )]
#endif
    private static void RestrictDirectoryToOwnerOnUnix( string directory )
    {
        // 0700: read/write/execute for the owner only.
#if NET
        File.SetUnixFileMode( directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute );
#else
        var setUnixFileMode = Array.Find(
            typeof(File).GetMethods(),
            m => m.Name == "SetUnixFileMode"
                 && m.GetParameters().Length == 2
                 && m.GetParameters()[0].ParameterType == typeof(string) );

        if ( setUnixFileMode == null )
        {
            return;
        }

        var unixFileModeType = setUnixFileMode.GetParameters()[1].ParameterType;

        // UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute == 0x100 | 0x80 | 0x40.
        var mode = Enum.ToObject( unixFileModeType, 0x100 | 0x80 | 0x40 );

        setUnixFileMode.Invoke( null, new[] { directory, mode } );
#endif
    }

    /// <summary>
    /// Determines whether the given directory grants write access to users other than the current one.
    /// On Windows, this inspects the directory ACL; on Unix, this inspects the group and other write mode bits.
    /// </summary>
    internal static bool IsDirectoryWritableByOtherUsers( string directory )
    {
        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            return IsDirectoryWritableByOtherUsersOnWindows( directory );
        }

        return IsDirectoryWritableByOtherUsersOnUnix( directory );
    }

#if NET
    [SupportedOSPlatform( "windows" )]
#endif
    private static bool IsDirectoryWritableByOtherUsersOnWindows( string directory )
    {
        var directoryInfo = new DirectoryInfo( directory );
        var security = directoryInfo.GetAccessControl();
        var trustedIdentities = GetTrustedWindowsIdentities();

        // Only atomic write/modify rights are listed. Composite rights such as Write, Modify and FullControl must NOT
        // be used here: their numeric value also includes read and execute bits, which would incorrectly flag a
        // directory that other users can only read. A composite grant is still detected because its value includes
        // these atomic bits (e.g. FullControl includes WriteData).
        const FileSystemRights writeRights =
            FileSystemRights.WriteData                      // Also known as CreateFiles.
            | FileSystemRights.AppendData                   // Also known as CreateDirectories.
            | FileSystemRights.WriteExtendedAttributes
            | FileSystemRights.WriteAttributes
            | FileSystemRights.Delete
            | FileSystemRights.DeleteSubdirectoriesAndFiles
            | FileSystemRights.ChangePermissions
            | FileSystemRights.TakeOwnership;

        foreach ( FileSystemAccessRule rule in security.GetAccessRules( true, true, typeof(SecurityIdentifier) ) )
        {
            if ( rule.AccessControlType != AccessControlType.Allow )
            {
                continue;
            }

            if ( (rule.FileSystemRights & writeRights) == 0 )
            {
                continue;
            }

            if ( rule.IdentityReference is SecurityIdentifier sid && !trustedIdentities.Contains( sid ) )
            {
                return true;
            }
        }

        return false;
    }

#if NET
    [SupportedOSPlatform( "windows" )]
#endif
    private static HashSet<SecurityIdentifier> GetTrustedWindowsIdentities()
    {
        using var identity = WindowsIdentity.GetCurrent();

        var trustedIdentities = new HashSet<SecurityIdentifier>
        {
            new( WellKnownSidType.LocalSystemSid, null ),
            new( WellKnownSidType.BuiltinAdministratorsSid, null ),

            // In an inherited ACE, CREATOR OWNER resolves to the owner of the object, i.e. the current user.
            new( WellKnownSidType.CreatorOwnerSid, null )
        };

        if ( identity.User != null )
        {
            trustedIdentities.Add( identity.User );
        }

        return trustedIdentities;
    }

#if NET
    [UnsupportedOSPlatform( "windows" )]
#endif
    private static bool IsDirectoryWritableByOtherUsersOnUnix( string directory )
    {
#if NET
        var mode = File.GetUnixFileMode( directory );

        return (mode & (UnixFileMode.GroupWrite | UnixFileMode.OtherWrite)) != 0;
#else
        // On netstandard2.0 (which may run on .NET 7 or later) the File.GetUnixFileMode method exists at run time
        // even though it is not part of the compile-time surface. We call it through reflection.
        // (net472 is Windows-only and never reaches this method.)
        var getUnixFileMode = typeof(File).GetMethod( "GetUnixFileMode", new[] { typeof(string) } );

        if ( getUnixFileMode == null )
        {
            // Unknown or old runtime: we cannot determine the mode, so we assume the directory is secure to avoid breaking the build.
            return false;
        }

        var mode = Convert.ToInt32( getUnixFileMode.Invoke( null, new object[] { directory } ), CultureInfo.InvariantCulture );

        // UnixFileMode.GroupWrite == 0x10, UnixFileMode.OtherWrite == 0x02.
        const int groupOrOtherWrite = 0x10 | 0x02;

        return (mode & groupOrOtherWrite) != 0;
#endif
    }
}
