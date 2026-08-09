// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Backstage.Configuration
{
    /// <summary>
    /// The exception thrown by <see cref="IConfigurationManager.TryUpdate"/> when a configuration file cannot be written
    /// because the global configuration mutex could not be acquired.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is an implementation detail of the optimistic update loop of
    /// <see cref="ConfigurationManagerExtensions.UpdateIf{T}"/> and <see cref="ConfigurationManagerExtensions.Update{T}"/>,
    /// which catch it and report that the configuration was not updated. It tells that loop that the update must be
    /// abandoned rather than retried, because every retry would wait for the same unavailable mutex.
    /// </para>
    /// <para>
    /// It is a distinct type, rather than a plain <see cref="TimeoutException"/>, so that the update loop does not also
    /// swallow a timeout raised by the update delegate or by the file system.
    /// </para>
    /// </remarks>
    internal sealed class ConfigurationMutexTimeoutException : TimeoutException
    {
        public ConfigurationMutexTimeoutException( string message ) : base( message ) { }
    }
}
