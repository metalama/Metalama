// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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