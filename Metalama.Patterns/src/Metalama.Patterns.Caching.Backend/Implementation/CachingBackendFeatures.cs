// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// List of features that can be implemented or not by a <see cref="CachingBackend"/>.
/// </summary>
[PublicAPI]
public class CachingBackendFeatures
{
    /// <summary>
    /// Gets a value indicating whether the <see cref="CachingBackend.Clear"/> method is supported.
    /// </summary>
    public virtual bool Clear => true;

    /// <summary>
    /// Gets a value indicating whether the <see cref="CachingBackend.ItemRemoved"/> and <see cref="CachingBackend.DependencyInvalidated"/> events are raised.
    /// </summary>
    public virtual bool Events => true;

    /// <summary>
    /// Gets a value indicating whether methods that modify the cache content run to completion before the control is given back to the calling method.
    /// If <c>false</c>, methods may run in the background, and the calling thread may not have a consistent view of the cache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a backend that returns <c>false</c> is used behind an in-memory L1 cache, added with
    /// <see cref="CachingBackendFactory.WithL1(OutOfProcessCachingBackendBuilder,LayeredCachingBackendConfiguration?)"/>,
    /// the L1 cache cannot know when a removal or an invalidation has reached the backend. It records the removal in memory
    /// for one minute, with a timestamp taken from the wall clock of the current machine. During that minute, a value read
    /// from the backend for the same key is returned only if its own timestamp, taken from the wall clock of the machine
    /// that wrote the value, is strictly greater than the timestamp of the removal. Otherwise the item is reported as absent.
    /// </para>
    /// <para>
    /// This rule assumes that the clocks of all machines sharing the backend are synchronized. A clock difference does not
    /// corrupt the cache, but it changes what a read returns. If the clock of the writing machine is behind the clock of the
    /// removing machine by some offset, a value written within that offset after the removal receives a timestamp that is not
    /// greater than the timestamp of the removal, and is reported as absent until the record expires. If the clock of the
    /// writing machine is ahead by some offset, a value written within that offset before the removal receives a greater
    /// timestamp, and is returned and stored in the L1 cache although the pending removal is about to delete it from the
    /// backend. That value then stays in the L1 cache until it expires under its own expiration policy, or until the L1 cache
    /// receives an <see cref="CachingBackend.ItemRemoved"/> or <see cref="CachingBackend.DependencyInvalidated"/> event for it.
    /// Two timestamps that are equal count as a removal. On .NET Framework, <see cref="DateTimeOffset.UtcNow"/> has a
    /// resolution of about fifteen milliseconds, so a value written by another machine shortly after the removal
    /// can receive the same timestamp as the removal, in which case it is reported as absent.
    /// </para>
    /// <para>
    /// Backends shipped with Metalama return <c>true</c>, so this rule applies only to custom backends.
    /// </para>
    /// </remarks>
    public virtual bool Blocking => true;

    /// <summary>
    /// Gets a value indicating whether dependencies are supported.
    /// </summary>
    public virtual bool Dependencies => true;

    /// <summary>
    /// Gets a value indicating whether the <see cref="CachingBackend.ContainsDependency(string)"/> method is supported.
    /// </summary>
    public virtual bool ContainsDependency => true;
}