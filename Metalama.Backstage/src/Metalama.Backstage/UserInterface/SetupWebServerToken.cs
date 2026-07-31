// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.UserInterface;

/// <summary>
/// Implements the per-session authentication token of the Backstage setup web server: the protocol by which the
/// process that starts the server hands the token to the server, and the name under which the token travels on
/// the URL given to the browser.
/// </summary>
/// <remarks>
/// <para>
/// The setup web server binds to the loopback interface only, but loopback is not a security boundary between local
/// accounts: on a shared machine (a terminal server, a shared build agent, a multi-user Unix box) any other local
/// user can reach the server and drive the current user's Backstage configuration. The token is what makes the
/// server usable only by the session that started it.
/// </para>
/// <para>
/// The token is deliberately not passed on the worker command line. A command line is readable by any local user
/// through <c>ps</c> or WMI, which is exactly the audience the token defends against; this is also why the port
/// itself is not treated as a secret. The token instead travels through a file whose <i>path</i> (not content) is
/// passed on the command line, and the server deletes that file as soon as it has read it.
/// </para>
/// </remarks>
internal static class SetupWebServerToken
{
    /// <summary>
    /// Number of random bytes in a token. 256 bits is far beyond brute-force reach for a server that lives about a minute.
    /// </summary>
    private const int _tokenByteCount = 32;

    /// <summary>
    /// The Unix permission bits allowing the owner, and only the owner, to read and write a file (<c>0600</c>).
    /// </summary>
    private const int _ownerReadWriteUnixFileMode = 0x180;

    /// <summary>
    /// Gets the name of the query-string parameter carrying the token on the URL handed to the browser.
    /// </summary>
    public const string QueryParameterName = "t";

    /// <summary>
    /// Generates a new token.
    /// </summary>
    public static string GenerateToken( RandomNumberGenerator randomNumberGenerator )
    {
        var bytes = new byte[_tokenByteCount];
        randomNumberGenerator.NextCryptographicBytes( bytes );

        // Base64url, so that the token requires no escaping when it is appended to a URL.
        return Convert.ToBase64String( bytes ).Replace( '+', '-' ).Replace( '/', '_' ).TrimEnd( '=' );
    }

    /// <summary>
    /// Writes <paramref name="token"/> to a new file under <paramref name="directory"/>, readable by the current user
    /// only, and returns the path of that file.
    /// </summary>
    public static string WriteTokenFile( string directory, string token )
    {
        Directory.CreateDirectory( directory );

        var path = Path.Combine( directory, $"setup-server-{Guid.NewGuid():N}.token" );

        // The file is created empty, then restricted, and only then filled, so that it never holds the token while it
        // is still readable by other users.
        using ( new FileStream( path, FileMode.CreateNew, FileAccess.Write, FileShare.None ) ) { }

        RestrictToCurrentUser( path );

        File.WriteAllText( path, token );

        return path;
    }

    /// <summary>
    /// Reads the token written by <see cref="WriteTokenFile"/> and deletes the file, so that the token exists on disk
    /// for as short a time as possible.
    /// </summary>
    public static string ReadTokenFile( string path )
    {
        var token = File.ReadAllText( path ).Trim();

        if ( token.Length == 0 )
        {
            throw new InvalidOperationException( $"The token file '{path}' is empty." );
        }

        File.Delete( path );

        return token;
    }

    /// <summary>
    /// Deletes the token file if it still exists, on a best-effort basis. Called by the process that started the server
    /// once it no longer needs the file, so that a token is not left on disk when the server failed to start and never
    /// consumed it.
    /// </summary>
    public static void DeleteTokenFile( string path, ILogger logger )
    {
        try
        {
            File.Delete( path );
        }
        catch ( Exception e ) when ( e is IOException or UnauthorizedAccessException )
        {
            logger.Warning?.Log( $"Cannot delete the setup server token file '{path}': {e.Message}" );
        }
    }

    /// <summary>
    /// Restricts a file to the current user on Unix. On Windows this is a no-op: the token file lives under the
    /// current user's local application data directory, whose inherited ACL already excludes other users.
    /// </summary>
    /// <remarks>
    /// <c>File.SetUnixFileMode</c> only exists on .NET 7.0 and later, but this assembly also targets netstandard2.0,
    /// which is loaded on such runtimes as well. The method is therefore resolved reflectively rather than through a
    /// compile-time conditional, so that the permissions are tightened whenever the running framework supports it.
    /// </remarks>
    private static void RestrictToCurrentUser( string path )
    {
        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            return;
        }

        var unixFileModeType = Type.GetType( "System.IO.UnixFileMode, System.Runtime" );

        var setUnixFileMode = unixFileModeType == null
            ? null
            : typeof(File).GetMethod(
                "SetUnixFileMode",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), unixFileModeType },
                null );

        setUnixFileMode?.Invoke( null, new[] { path, Enum.ToObject( unixFileModeType!, _ownerReadWriteUnixFileMode ) } );
    }
}
