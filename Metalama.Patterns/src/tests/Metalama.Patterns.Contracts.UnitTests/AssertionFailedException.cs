// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.Serialization;

namespace Metalama.Patterns.Contracts.UnitTests;

/// <summary>
/// Exception thrown upon internal assertion failures in Metalama Pattern Libraries.
/// </summary>
[Serializable]
public sealed class AssertionFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssertionFailedException"/> class with the default error message.
    /// </summary>
    public AssertionFailedException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssertionFailedException"/> class and specifies the error message.
    /// </summary>
    public AssertionFailedException( string message ) : base( message ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssertionFailedException"/> class and specifies the error message and the inner <see cref="Exception"/>.
    /// </summary>
    public AssertionFailedException( string message, Exception inner ) : base( message, inner ) { }

    private AssertionFailedException(
        SerializationInfo info,
        StreamingContext context ) : base( info, context ) { }
}