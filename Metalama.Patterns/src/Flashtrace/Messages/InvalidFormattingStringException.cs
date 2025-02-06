// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Messages;

/// <summary>
/// Exception thrown by the <see cref="FormattingStringParser"/> and by the <c>Logger</c> class
/// when user code provides an invalid formatting string.
/// </summary>
[PublicAPI]
[Serializable]
public sealed class InvalidFormattingStringException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFormattingStringException"/> class with the default error message.
    /// </summary>
    public InvalidFormattingStringException() : base( "Invalid formatting string." ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFormattingStringException"/> class specifying the error message. 
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidFormattingStringException( string message ) : base( message ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFormattingStringException"/> class specifying the error message and 
    /// the inner <see cref="Exception"/>.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="inner"></param>
    public InvalidFormattingStringException( string message, Exception inner ) : base( message, inner ) { }
}