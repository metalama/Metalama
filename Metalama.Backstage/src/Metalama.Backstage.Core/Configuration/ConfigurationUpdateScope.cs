// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using System;

namespace Metalama.Backstage.Configuration;

/// <summary>
/// The rules that every implementation of <see cref="IConfigurationManager"/> must follow when it runs a
/// transformation and when it announces a change.
/// </summary>
/// <remarks>
/// The rules live here, and not in each implementation, because a substitute that enforces them less strictly than
/// the real implementation is worse than no substitute at all: a test written against it passes while the product
/// fails. Both <see cref="ConfigurationManager"/> and <see cref="InMemoryConfigurationManager"/> therefore call
/// into this class rather than reproducing the behaviour.
/// </remarks>
internal static class ConfigurationUpdateScope
{
    /// <summary>
    /// The file whose transformation the current thread is executing, or <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// A transformation of the real implementation runs while the lock protecting its file is held, so one that
    /// started a second update would make the thread hold two named locks at once, which deadlocks as soon as
    /// another thread takes the same two in the opposite order. The field is static, and therefore shared by every
    /// manager in the process, because two managers over the same directory take the same locks.
    /// </remarks>
    [ThreadStatic]
    private static string? _fileBeingUpdatedByCurrentThread;

    /// <summary>
    /// Verifies that the current thread is not already executing a transformation, and throws if it is.
    /// </summary>
    /// <param name="fileName">The file that the caller is about to update.</param>
    /// <remarks>
    /// Refused rather than allowed to nest, because the alternative is a deadlock between two processes that is
    /// neither reproducible nor diagnosable. A transformation may read any configuration file, which takes no
    /// lock, but it may not update one.
    /// </remarks>
    public static void VerifyNotNested( string fileName )
    {
        if ( _fileBeingUpdatedByCurrentThread != null )
        {
            throw new InvalidOperationException(
                $"Cannot update '{fileName}' from within the transformation of '{_fileBeingUpdatedByCurrentThread}'. "
                + "A transformation runs while the lock protecting its own file is held, and the locks of this class are not reentrant." );
        }
    }

    /// <summary>
    /// Marks the current thread as executing the transformation of a given file, until the returned object is
    /// disposed.
    /// </summary>
    /// <param name="fileName">The file being transformed.</param>
    /// <returns>An object that clears the mark.</returns>
    /// <remarks>
    /// The mark covers exactly the transformation, and not the dispatch of the event that follows the release of
    /// the lock: a handler is free to update whatever it likes, precisely because it holds nothing when it runs.
    /// </remarks>
    public static IDisposable Enter( string fileName )
    {
        _fileBeingUpdatedByCurrentThread = fileName;

        return new Scope();
    }

    /// <summary>
    /// Announces a change to the subscribers of an event, one handler at a time.
    /// </summary>
    /// <param name="handlers">The multicast delegate, or <see langword="null"/> if nothing is subscribed.</param>
    /// <param name="value">The new value of the configuration file.</param>
    /// <param name="logger">The logger receiving the exception of a handler that throws.</param>
    /// <remarks>
    /// The handlers are invoked one by one rather than through the multicast delegate, because a multicast
    /// invocation stops at the first handler that throws, so one faulty subscriber would silently deprive every
    /// subsequent one of the notification.
    /// </remarks>
    public static void RaiseConfigurationFileChanged( Action<ConfigurationFile>? handlers, ConfigurationFile value, ILogger logger )
    {
        if ( handlers == null )
        {
            return;
        }

        foreach ( var handler in handlers.GetInvocationList() )
        {
            try
            {
                ((Action<ConfigurationFile>) handler).Invoke( value );
            }
            catch ( Exception e )
            {
                logger.LogException( e, "Error in a handler of IConfigurationManager.ConfigurationFileChanged" );
            }
        }
    }

    /// <summary>
    /// Clears the mark set by <see cref="Enter"/>.
    /// </summary>
    private sealed class Scope : IDisposable
    {
        /// <inheritdoc />
        public void Dispose() => _fileBeingUpdatedByCurrentThread = null;
    }
}
