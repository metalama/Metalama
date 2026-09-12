// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Configuration;

/// <summary>
/// The result of an attempt to update a configuration file.
/// </summary>
/// <remarks>
/// Only <see cref="Updated"/> means that the file was written. The other members are all ordinary outcomes and none
/// of them is an error: failing to write a configuration file must never fail a compilation.
/// </remarks>
[PublicAPI]
public enum ConfigurationUpdateOutcome
{
    /// <summary>
    /// The file was written.
    /// </summary>
    Updated,

    /// <summary>
    /// The transformation declined to produce a new value, having found that the file did not need to be updated.
    /// </summary>
    Declined,

    /// <summary>
    /// The transformation produced a value equal to the one the file already held.
    /// </summary>
    NoChange,

    /// <summary>
    /// The lock protecting the file could not be acquired, so nothing was read and nothing was written.
    /// </summary>
    LockTimeout,

    /// <summary>
    /// The file could not be written. Its previous content is intact, because the write is atomic.
    /// </summary>
    WriteFailed
}
