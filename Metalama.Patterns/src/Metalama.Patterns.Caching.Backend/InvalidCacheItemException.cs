// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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