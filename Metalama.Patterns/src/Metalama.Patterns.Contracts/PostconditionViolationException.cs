// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Runtime.Serialization;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// The exception that is thrown when a postcondition contract was not fulfilled by a method.
/// </summary>
[Serializable]
public class PostconditionViolationException : ApplicationException
{
    /// <summary>
    /// Gets the value that caused this exception, when applicable.
    /// </summary>
    public object? ActualValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostconditionViolationException"/> class.
    /// </summary>
    public PostconditionViolationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostconditionViolationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public PostconditionViolationException( string message )
        : base( message ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostconditionViolationException"/> class with a specified error message
    /// and actual value.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="actualValue">The value that caused the exception.</param>
    public PostconditionViolationException( string message, object? actualValue )
        : base( message )
    {
        this.ActualValue = actualValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostconditionViolationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="innerException">Inner exception.</param>
    public PostconditionViolationException( string message, Exception? innerException )
        : base( message, innerException ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostconditionViolationException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    protected PostconditionViolationException( SerializationInfo info, StreamingContext context )
        : base( info, context ) { }
}