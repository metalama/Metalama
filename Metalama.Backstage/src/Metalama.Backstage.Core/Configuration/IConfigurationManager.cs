// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Configuration
{
    [PublicAPI]
    public interface IConfigurationManager : IDisposable, IBackstageService
    {
        ILogger Logger { get; }

        string GetFilePath( string fileName );

        string GetFilePath( Type type );

        ConfigurationFile Get( Type type, bool ignoreCache = false );

        event Action<ConfigurationFile> ConfigurationFileChanged;

        /// <summary>
        /// Reads a configuration file, transforms it and writes the result, as a single transaction.
        /// </summary>
        /// <param name="type">The type of the configuration file.</param>
        /// <param name="transform">
        /// Produces the new content of the file from its current content, or returns <see langword="null"/> to
        /// decline the update.
        /// </param>
        /// <returns>What happened, of which only <see cref="ConfigurationUpdateOutcome.Updated"/> means that the file was written.</returns>
        /// <remarks>
        /// <para>
        /// The whole read, transformation and write runs while the lock protecting the file is held, so the value
        /// <paramref name="transform"/> receives is the content of the file at the moment of the write and cannot
        /// become stale in between. This is what replaces the optimistic loop the callers used to run, in which
        /// each attempt read, serialized and compared before discovering that another writer had intervened.
        /// </para>
        /// <para>
        /// <paramref name="transform"/> is called at most once, and it is called while the lock is held, so it must
        /// be a pure and fast computation over the value it receives. It may read any configuration file, because a
        /// read takes no lock, but it must not update one: it would then hold two locks at once, which deadlocks
        /// against a thread taking the same two in the opposite order. Attempting it raises an
        /// <see cref="InvalidOperationException"/> rather than being allowed to nest.
        /// </para>
        /// </remarks>
        ConfigurationUpdateOutcome Update( Type type, Func<ConfigurationFile, ConfigurationFile?> transform );
    }
}