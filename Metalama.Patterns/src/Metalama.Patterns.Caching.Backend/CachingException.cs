// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.Serialization;

namespace Metalama.Patterns.Caching;

/// <summary>
/// Exception thrown by <c>Metalama.Patterns.Caching</c>.
/// </summary>
[Serializable]
public class CachingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CachingException"/> class with the default error message.
    /// </summary>
    public CachingException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingException"/> class with a given error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CachingException( string message ) : base( message ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingException"/> class with a given error message and inner <see cref="Exception"/>. 
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="inner">The inner exception.</param>
    public CachingException( string message, Exception inner ) : base( message, inner ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingException"/> class.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected CachingException(
        SerializationInfo info,
        StreamingContext context ) : base( info, context ) { }
}