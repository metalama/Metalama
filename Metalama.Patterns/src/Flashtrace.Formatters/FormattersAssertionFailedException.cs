// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.Serialization;

namespace Flashtrace.Formatters;

/// <summary>
/// Exception thrown upon internal assertion failures in the Flashtrace.Formatters library.
/// </summary>
/// <remarks>
/// Throw <see cref="FormattersAssertionFailedException"/> instead of using <see cref="System.Diagnostics.Debug"/>
/// assert methods so that the compiler can track execution flow.
/// </remarks>
[Serializable]
internal sealed class FormattersAssertionFailedException
    : ApplicationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormattersAssertionFailedException"/> class with the default error message.
    /// </summary>
    public FormattersAssertionFailedException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormattersAssertionFailedException"/> class specifying the error message.
    /// </summary>
    public FormattersAssertionFailedException( string message ) : base( message ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormattersAssertionFailedException"/> class specifying the error message and the inner <see cref="Exception"/>.
    /// </summary>
    public FormattersAssertionFailedException( string message, Exception inner ) : base( message, inner ) { }

    private FormattersAssertionFailedException(
        SerializationInfo info,
        StreamingContext context ) : base( info, context ) { }
}