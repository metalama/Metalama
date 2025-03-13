// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.Serialization;

namespace Metalama.Patterns.Caching;

/// <summary>
/// Exception thrown by a caching back-end during cache item retrieval (e.g. when the cached data cannot be serialized by the current object model).
/// Throwing this exception causes removal of the invalid item.
/// </summary>
[Serializable]
public class InvalidCacheItemException : CachingException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCacheItemException"/> class with the default error message.
    /// </summary>
    public InvalidCacheItemException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCacheItemException"/> class with a given error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidCacheItemException( string message ) : base( message ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCacheItemException"/> class with a given error message and inner <see cref="Exception"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="inner">The inner exception.</param>
    public InvalidCacheItemException( string message, Exception inner ) : base( message, inner ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCacheItemException"/> class.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected InvalidCacheItemException(
        SerializationInfo info,
        StreamingContext context ) : base( info, context ) { }
}