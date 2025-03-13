// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.Serialization;

namespace Flashtrace.Formatters;

/// <summary>
/// The exception that is thrown when getting an <see cref="IFormatter"/> from an <see cref="IFormatterRepository"/> when the formatter for the type is not found.
/// </summary>
[Serializable]
public sealed class FormatterNotFoundException : KeyNotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatterNotFoundException"/> class with the default error message.
    /// </summary>
    public FormatterNotFoundException()
        : base( "The repository was unable to provide a formatter for the requested type." ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatterNotFoundException"/> class with the specified error message.
    /// </summary>
    /// <param name="message"></param>
    public FormatterNotFoundException( string message ) : base( message ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatterNotFoundException"/> class with the specified error message and inner exception.
    /// </summary>
    public FormatterNotFoundException( string message, Exception innerException ) : base( message, innerException ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatterNotFoundException"/> class.
    /// </summary>
    public FormatterNotFoundException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }
}