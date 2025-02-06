// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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