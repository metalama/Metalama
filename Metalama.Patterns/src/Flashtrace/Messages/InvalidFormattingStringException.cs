// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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