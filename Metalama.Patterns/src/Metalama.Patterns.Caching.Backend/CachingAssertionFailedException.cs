// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Runtime.Serialization;

namespace Metalama.Patterns.Caching;

/// <summary>
/// Exception thrown upon internal assertion failures in the Flashtrace.Formatters library.
/// </summary>
/// <remarks>
/// Throw <see cref="CachingAssertionFailedException"/> instead of using <see cref="System.Diagnostics.Debug"/>
/// assert methods so that the compiler can track execution flow.
/// </remarks>
[Serializable]
[RunTimeOrCompileTime]
public sealed class CachingAssertionFailedException
    : ApplicationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CachingAssertionFailedException"/> class with the default error message.
    /// </summary>
    public CachingAssertionFailedException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingAssertionFailedException"/> class specifying the error message.
    /// </summary>
    public CachingAssertionFailedException( string message ) : base( message ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingAssertionFailedException"/> class specifying the error message and the inner <see cref="Exception"/>.
    /// </summary>
    public CachingAssertionFailedException( string message, Exception inner ) : base( message, inner ) { }

    private CachingAssertionFailedException(
        SerializationInfo info,
        StreamingContext context ) : base( info, context ) { }
}