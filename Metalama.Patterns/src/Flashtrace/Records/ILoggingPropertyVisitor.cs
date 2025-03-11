// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// Defines a visit method invoked for each property of a <see cref="LogEventData"/>.
/// </summary>
/// <typeparam name="TState">Type of an opaque value passed to the <see cref="Visit{TValue}"/> method.
/// </typeparam>
[PublicAPI]
public interface ILoggingPropertyVisitor<TState>
{
    /// <summary>
    /// Method invoked for each property in a <see cref="LogEventData"/>.
    /// </summary>
    /// <typeparam name="TValue">Type of the property.</typeparam>
    /// <param name="name">Property name.</param>
    /// <param name="value">Property value.</param>
    /// <param name="options">Property options.</param>
    /// <param name="state">State passed from the caller through the <see cref="LogEventData.VisitProperties{TVisitorState}"/> method.</param>
    void Visit<TValue>( string name, TValue? value, in LoggingPropertyOptions options, ref TState state );
}