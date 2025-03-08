// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Metalama.Backstage.Utilities;

public static class MutexHelper
{
    private static readonly object _sync = new();
    private static bool? _isThreadingAccessControlAvailable;

    public static IDisposable WithGlobalLock( string name, ILogger? logger = null )
    {
        logger?.Trace?.Log( $"Acquiring lock '{name}'." );

        var mutex = OpenOrCreateGlobalMutex( name, logger );

        try
        {
            if ( !mutex.WaitOne( 0 ) )
            {
                logger?.Trace?.Log( $"  Another process owns '{name}'. Waiting." );
                mutex.WaitOne();
            }
        }
        catch ( AbandonedMutexException )
        {
            logger?.Warning?.Log( "  Ignoring an AbandonedMutexException." );
        }

        logger?.Trace?.Log( $"Lock '{name}' acquired." );

        return new MutexHandle( mutex, name, logger );
    }

    internal static IDisposable? WithLock( string name, string? prefix, TimeSpan timeout, ILogger? logger = null )
    {
        var mutex = OpenOrCreateMutex( name, prefix, logger );

        bool acquired;

        try
        {
            acquired = mutex.WaitOne( timeout );
        }
        catch ( AbandonedMutexException )
        {
            acquired = true;

            logger?.Warning?.Log( "  Ignoring an AbandonedMutexException." );
        }

        if ( acquired )
        {
            logger?.Trace?.Log( $"Lock '{name}' acquired." );

            return new MutexHandle( mutex, name, logger );
        }
        else
        {
            logger?.Trace?.Log( $"Lock '{name}' not acquired." );

            return null;
        }
    }

    private static Mutex OpenOrCreateGlobalMutex( string fullName, ILogger? logger ) => OpenOrCreateMutex( fullName, prefix: null, logger );

    internal static Mutex OpenOrCreateMutex( string fullName, string? prefix, ILogger? logger )
    {
        prefix ??= "Global\\Metalama_";
        var mutexName = prefix + HashUtilities.HashToString( fullName );

        return OpenOrCreateMutex( mutexName, logger );
    }

    // This code is duplicated in DesignTimeEntryPointManager in Metalama.Framework and should be kept in sync.
    private static Mutex OpenOrCreateMutex( string mutexName, ILogger? logger )
    {
        logger?.Trace?.Log( $"  Mutex name: '{mutexName}'." );

        // Avoid concurrency within the current process in creating mutexes.
        // This is an attempt to make debugging easier if there are issues in this method.
        lock ( _sync )
        {
            // The number of iterations is intentionally very low.
            // We will restart if the following occurs:
            //   1) TryOpenExisting fails, i.e. there is no existing mutex.
            //   2) Creating a new mutex fails, i.e. the mutex was created in the meantime by a process with higher set of rights.
            // The probability of mutex being destroyed when we call TryOpenExisting again is fairly low.

            for ( var i = 0; /* Intentionally empty */; i++ )
            {
                // First try opening the mutex.
                if ( Mutex.TryOpenExisting( mutexName, out var existingMutex ) )
                {
                    logger?.Trace?.Log( "  Opened existing mutex." );

                    return existingMutex;
                }
                else
                {
                    // Otherwise we will try to create the mutex.
                    try
                    {
                        if ( IsThreadingAccessControlAvailable() )
                        {
                            try
                            {
                                // Based on https://stackoverflow.com/a/19717341/41071.
                                // As I understand it, creating a mutex without security descriptor uses default security, which could be different on different systems.
                                // I'm not certain this will actually prevent UnauthorizedAccessException, but it's worth trying.

                                logger?.Trace?.Log( "  Creating new mutex with access rule." );

#pragma warning disable CA1416
                                var mutexSecurity = new MutexSecurity();

                                mutexSecurity.AddAccessRule(
                                    new MutexAccessRule(
                                        new SecurityIdentifier( WellKnownSidType.WorldSid, null ),
                                        MutexRights.Synchronize | MutexRights.Modify,
                                        AccessControlType.Allow ) );

                                return MutexAcl.Create( false, mutexName, out _, mutexSecurity );
#pragma warning restore CA1416
                            }
                            catch ( PlatformNotSupportedException e )
                            {
                                logger?.Warning?.Log( e.ToString() );
                                
                                // Disabling for further calls.
                                _isThreadingAccessControlAvailable = false;
                            }
                        }

                        logger?.Trace?.Log( "  Creating new mutex without access rules." );

                        return new Mutex( false, mutexName );
                    }
                    catch ( UnauthorizedAccessException )
                    {
                        if ( i < 3 )
                        {
                            Thread.Sleep( 0 );

                            // Mutex was probably created in the meantime and is not accessible - we will restart.
                            logger?.Trace?.Log( "  Mutex was probably created and current process has restricted access to it, restarting." );
                        }
                        else
                        {
                            // There were too many restarts - just rethrow.
                            logger?.Trace?.Log( "  Tried to open mutex too many times - throwing the exception received." );

                            throw;
                        }
                    }
                }
            }
        }
    }

    private static bool IsThreadingAccessControlAvailable()
    {
        return _isThreadingAccessControlAvailable ??= RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) &&
                                                      (RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ) ||
                                                       (RuntimeInformation.FrameworkDescription.StartsWith( ".NET", StringComparison.Ordinal )
                                                        && Environment.Version.Major >= 8));
    }
}