// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using System;
using System.IO;
#if !NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Metalama.Backstage.Utilities;

/// <summary>
/// Writes a text file so that a concurrent reader observes either the whole previous content or the whole new content,
/// and tolerates the conditions under which the operating system refuses to substitute the file.
/// </summary>
/// <remarks>
/// <para>
/// The content is written to a temporary file next to the destination, which is then substituted for the destination.
/// The substitution is attempted with <see cref="File.Replace(string,string,string)"/> first. On Windows,
/// <c>ReplaceFile</c> fails with <see cref="UnauthorizedAccessException"/> or <see cref="IOException"/> when another
/// process holds the destination without <see cref="FileShare.Delete"/>, when the destination is pending deletion, or
/// when its access control list cannot be copied. The caller can then allow the strategies of
/// <see cref="AtomicFileWriterFallbacks"/>, each of which gives up a guarantee of <c>File.Replace</c>: a rename that
/// overwrites the destination, which does not preserve its access control list, and an in-place write, which only
/// requires readers to share writing but is not atomic. See https://github.com/metalama/Metalama/issues/2317.
/// </para>
/// <para>
/// The whole operation is retried for a short while. The temporary file of every attempt is removed, even when the
/// attempt fails.
/// </para>
/// </remarks>
[PublicAPI]
public static class AtomicFileWriter
{
    /// <summary>
    /// Attempts to write <paramref name="content"/> to <paramref name="path"/>, and never throws an
    /// <see cref="IOException"/> or an <see cref="UnauthorizedAccessException"/>.
    /// </summary>
    /// <param name="path">The file to write to.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="fallbacks">
    /// The strategies allowed when <see cref="File.Replace(string,string,string)"/> fails. See
    /// <see cref="AtomicFileWriterFallbacks"/> for the guarantee that each of them gives up.
    /// </param>
    /// <param name="logger">The logger, or <see langword="null"/>.</param>
    /// <param name="exception">The exception of the last attempt when the method returns <see langword="false"/>.</param>
    /// <param name="beforeSubstitution">
    /// An action invoked once the temporary file has been written and the existence of the destination has been
    /// established, but before the destination is substituted. Used by tests.
    /// </param>
    /// <returns><see langword="true"/> if the file has been written, otherwise <see langword="false"/>.</returns>
    public static bool TryWriteAllText(
        string path,
        string? content,
        AtomicFileWriterFallbacks fallbacks,
        ILogger? logger,
        out Exception? exception,
        Action? beforeSubstitution = null )
    {
        try
        {
            RetryHelper.Retry( () => WriteOnce( path, content, fallbacks, logger, beforeSubstitution ), logger: logger );

            exception = null;

            return true;
        }
        catch ( Exception e ) when ( e is IOException or UnauthorizedAccessException )
        {
            logger?.Warning?.Log( $"Cannot write '{path}': {e.GetType().Name}: {e.Message}" );

            exception = e;

            return false;
        }
    }

    private static void WriteOnce( string path, string? content, AtomicFileWriterFallbacks fallbacks, ILogger? logger, Action? beforeSubstitution )
    {
        // A new temporary file for every attempt: the previous one has been deleted, and two attempts that were to
        // reuse a name would depend on that deletion having completed.
        var tempPath = GetTemporarySiblingPath( path );

        try
        {
            File.WriteAllText( tempPath, content );

            var destinationExists = File.Exists( path );

            beforeSubstitution?.Invoke();

            if ( !destinationExists )
            {
                // File.Move throws when the destination exists, which is what makes this branch safe: if the
                // destination appeared between the test above and this call, the next attempt finds it and substitutes
                // it properly instead of this one failing to create it.
                File.Move( tempPath, path );

                return;
            }

            try
            {
                // File.Replace preserves the access control list of the destination.
                File.Replace( tempPath, path, destinationBackupFileName: null );

                return;
            }
            catch ( Exception replaceException ) when ( replaceException is IOException or UnauthorizedAccessException )
            {
                logger?.Trace?.Log( $"Cannot replace '{path}': {replaceException.GetType().Name}: {replaceException.Message}." );

                if ( (fallbacks & AtomicFileWriterFallbacks.RenameOverwrite) != 0 && TryMoveWithOverwrite( tempPath, path, logger ) )
                {
                    return;
                }

                if ( (fallbacks & AtomicFileWriterFallbacks.WriteInPlace) == 0 )
                {
                    throw;
                }

                logger?.Warning?.Log( $"Cannot substitute '{path}' atomically. Writing it in place." );

                File.WriteAllText( path, content );
            }
        }
        finally
        {
            if ( File.Exists( tempPath ) )
            {
                File.Delete( tempPath );
            }
        }
    }

    private static bool TryMoveWithOverwrite( string source, string destination, ILogger? logger )
    {
#if NET5_0_OR_GREATER
        try
        {
            File.Move( source, destination, overwrite: true );

            return true;
        }
        catch ( Exception e ) when ( e is IOException or UnauthorizedAccessException )
        {
            logger?.Trace?.Log( $"Cannot rename '{source}' over '{destination}': {e.GetType().Name}: {e.Message}" );

            return false;
        }
#else
        if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            return false;
        }

        if ( NativeMethods.MoveFileEx( source, destination, NativeMethods.MoveFileReplaceExisting | NativeMethods.MoveFileWriteThrough ) )
        {
            return true;
        }

        logger?.Trace?.Log( $"Cannot rename '{source}' over '{destination}': Win32 error {Marshal.GetLastWin32Error()}." );

        return false;
#endif
    }

    /// <summary>
    /// Returns the path of a file that does not exist, located in the same directory as <paramref name="path"/>.
    /// </summary>
    /// <remarks>
    /// The name begins with a period, so that the file is hidden on Unix, and ends with <c>.tmp</c>, so that a
    /// caller enumerating the directory by the extension of the destination does not observe it. The identifier
    /// in the middle makes two concurrent writers use two different temporary files.
    /// </remarks>
    private static string GetTemporarySiblingPath( string path )
        => Path.Combine( Path.GetDirectoryName( path ) ?? ".", $".{Path.GetFileName( path )}.{Guid.NewGuid():N}.tmp" );

#if !NET5_0_OR_GREATER
    private static class NativeMethods
    {
        public const int MoveFileReplaceExisting = 0x1;
        public const int MoveFileWriteThrough = 0x8;

        [DllImport( "kernel32.dll", EntryPoint = "MoveFileExW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true )]
        [return: MarshalAs( UnmanagedType.Bool )]
        public static extern bool MoveFileEx( string existingFileName, string newFileName, int flags );
    }
#endif
}
