// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Contracts;

/// <summary>
/// An exception that should be thrown by the target methods of the <see cref="InvariantAttribute"/> aspect
/// when an invariant condition is violated.
/// </summary>
/// <remarks>
/// <para>Methods marked with <see cref="InvariantAttribute"/> are automatically called after public or internal methods
/// complete. When an invariant method detects an inconsistent state, it should throw this exception to indicate
/// the contract violation.</para>
/// </remarks>
/// <seealso cref="InvariantAttribute"/>
/// <seealso cref="SuspendInvariantsAttribute"/>
/// <seealso href="@invariants"/>
public class InvariantViolationException : ApplicationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvariantViolationException"/> class.
    /// </summary>
    public InvariantViolationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvariantViolationException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the invariant violation.</param>
    public InvariantViolationException( string message ) : base( message ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvariantViolationException"/> class
    /// with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the invariant violation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvariantViolationException( string message, Exception innerException ) : base( message, innerException ) { }
}