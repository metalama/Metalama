// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Records;
using JetBrains.Annotations;

namespace Flashtrace.Options;

// TODO: Reconsider mutability.
/// <summary>
/// Options for the <see cref="Exception"/> method.
/// </summary>
[PublicAPI]
public readonly struct WriteMessageOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WriteMessageOptions"/> struct optionally specifying the properties with a CLR object.
    /// </summary>
    /// <param name="data">Optional. Specifies the properties of the <see cref="WriteMessageOptions"/>, typically specified as an instance of a well-known or anonymous CLR type.
    /// The resulting <see cref="LogEventData"/> will have the default <see cref="LogEventMetadata"/>, which means that all CLR properties will be exposed
    /// as logging properties unless they are annotated with <see cref="LoggingPropertyOptionsAttribute"/>.</param>
    public WriteMessageOptions( object? data = null ) : this( LogEventData.Create( data ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="WriteMessageOptions"/> struct specifying the properties with a <see cref="LogEventData"/>.
    /// </summary>
    /// <param name="data">Specifies the properties of the <see cref="WriteMessageOptions"/>. See <see cref="LogEventData"/>.</param>
    public WriteMessageOptions( in LogEventData data ) : this()
    {
        this.Data = data;
    }

    /// <summary>
    /// Gets the properties of the <see cref="WriteMessageOptions"/>, typically specified as an instance of a well-known or anonymous CLR type.
    /// </summary>
    public LogEventData Data { get; init; }
}