// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Utilities;
using System;
using System.Threading;

namespace Metalama.Backstage.Threading;

/// <summary>
/// Composes the names of the locks used by Metalama, and offers the shorthands for the components that acquire a
/// name exactly once.
/// </summary>
/// <remarks>
/// <para>
/// This class is deliberately absent from the source files that <c>Metalama.Framework.CompilerExtensions</c> and
/// <c>Metalama.Framework.DesignTime.Contracts</c> compile, because it depends on the hashing library and because
/// those projects compose their lock names differently: the resource extractor hashes with SHA-256, which it can
/// reach without a dependency, and the design-time entry point manager uses a name that must stay verbatim.
/// </para>
/// </remarks>
[PublicAPI]
public static class NamedLockExtensions
{
    /// <summary>
    /// The prefix of the operating system objects backing the locks of Metalama. The <c>Global\</c> namespace
    /// makes them shared by every session of the machine.
    /// </summary>
    private const string _globalLockNamePrefix = "Global\\Metalama_";

    /// <summary>
    /// Composes the name of the operating system object backing the lock protecting a given resource.
    /// </summary>
    /// <param name="resourceName">
    /// The resource being protected, usually the full path of a file or of a directory.
    /// </param>
    /// <returns>The name of the operating system object.</returns>
    /// <remarks>
    /// The name is hashed because a path can exceed the length an operating system object name may have, and
    /// because a path can contain characters that such a name may not.
    /// </remarks>
    public static string GetGlobalLockName( string resourceName ) => _globalLockNamePrefix + HashUtilities.HashToString( resourceName );

    /// <summary>
    /// Gets the lock protecting a given resource, without acquiring it.
    /// </summary>
    /// <param name="service">The lock service.</param>
    /// <param name="resourceName">The resource being protected, usually the full path of a file or of a directory.</param>
    /// <param name="cancellationToken">A token that aborts the creation of the operating system object.</param>
    /// <returns>The lock. The caller must dispose it when it no longer needs it.</returns>
    /// <remarks>
    /// This is the form to use in a component that locks the same resource repeatedly, so that the operating
    /// system object is opened once and kept.
    /// </remarks>
    public static INamedLock GetGlobalLock( this INamedLockService service, string resourceName, CancellationToken cancellationToken = default )
        => service.GetLock( GetGlobalLockName( resourceName ), cancellationToken );

    /// <summary>
    /// Gets the lock protecting a given resource and acquires it, waiting for at most a given time, and throws if
    /// it cannot be acquired.
    /// </summary>
    /// <param name="service">The lock service.</param>
    /// <param name="resourceName">The resource being protected, usually the full path of a file or of a directory.</param>
    /// <param name="timeout">The maximal waiting time, or <see langword="null"/> to wait indefinitely.</param>
    /// <param name="cancellationToken">A token that aborts the wait.</param>
    /// <returns>An object that releases the lock and disposes it.</returns>
    /// <remarks>
    /// This is the form to use in a component that locks an arbitrary resource once, which is why the returned
    /// object owns the lock as well as the acquisition. A component that locks the same resource repeatedly uses
    /// <see cref="GetGlobalLock"/> instead.
    /// </remarks>
    public static IDisposable WithGlobalLock(
        this INamedLockService service,
        string resourceName,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default )
    {
        var @lock = service.GetGlobalLock( resourceName, cancellationToken );

        try
        {
            return new LockScope( @lock, @lock.Acquire( timeout, cancellationToken ) );
        }
        catch
        {
            @lock.Dispose();

            throw;
        }
    }

    /// <summary>
    /// Gets the lock protecting a given resource and attempts to acquire it, waiting for at most a given time.
    /// </summary>
    /// <param name="service">The lock service.</param>
    /// <param name="resourceName">The resource being protected, usually the full path of a file or of a directory.</param>
    /// <param name="timeout">The maximal waiting time.</param>
    /// <param name="cancellationToken">A token that aborts the wait.</param>
    /// <returns>
    /// An object that releases the lock and disposes it, or <see langword="null"/> if the lock could not be
    /// acquired within <paramref name="timeout"/>, in which case nothing needs to be disposed.
    /// </returns>
    public static IDisposable? TryWithGlobalLock(
        this INamedLockService service,
        string resourceName,
        TimeSpan timeout,
        CancellationToken cancellationToken = default )
    {
        var @lock = service.GetGlobalLock( resourceName, cancellationToken );

        try
        {
            if ( !@lock.TryAcquire( timeout, out var releaser, cancellationToken ) )
            {
                @lock.Dispose();

                return null;
            }

            return new LockScope( @lock, releaser );
        }
        catch
        {
            @lock.Dispose();

            throw;
        }
    }

    /// <summary>
    /// Releases an acquisition and disposes the lock it was made on, for the callers that create, use and dispose
    /// a lock in a single operation.
    /// </summary>
    private sealed class LockScope : IDisposable
    {
        private readonly INamedLock _lock;
        private readonly IDisposable _releaser;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockScope"/> class.
        /// </summary>
        /// <param name="lock">The lock to dispose.</param>
        /// <param name="releaser">The acquisition to release.</param>
        public LockScope( INamedLock @lock, IDisposable releaser )
        {
            this._lock = @lock;
            this._releaser = releaser;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // The order matters: the acquisition is released while the operating system object still exists.
            this._releaser.Dispose();
            this._lock.Dispose();
        }
    }
}
