// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Runtime.CompilerServices;

namespace Flashtrace.Messages;

/// <summary>
/// Creates semantic messages composed of a message name and a list of properties given as name-value pairs. These messages are ideal for machine analysis.
/// For more succinct code, consider including the <c>using static Flashtrace.SemanticMessageBuilder</c> statement.
/// </summary>
[PublicAPI]
public static partial class SemanticMessageBuilder
{
    /// <summary>
    /// Creates a semantic message with an arbitrary number of parameters.
    /// </summary>
    /// <param name="messageName">Message name.</param>
    /// <param name="parameters">Array of parameters (name-value pairs).</param>
    /// <returns></returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )] // To avoid copying the struct.
    // Intentionally removing the params modifier because otherwise the C# compiler picks the wrong overload which causes a performance issue.
    public static SemanticMessageArray Semantic( string messageName, (string Name, object? Value)[] parameters ) => new( messageName, parameters );

    /// <summary>
    /// Create a semantic message with 1 parameter, using tuples, where the value is of type <see cref="object"/>.
    /// </summary>
    /// <param name="name">Name of the message.</param>
    /// <param name="parameter">Name and value of the first parameter wrapped as a tuple.</param>
    [MethodImpl( MethodImplOptions.AggressiveInlining )] // To avoid copying the struct.
    public static SemanticMessage<object> Semantic( string name, in (string Name, object? Value) parameter )
    {
        // NB: Without this overload, the C# compiler tries to use the (string Name, object Value)[] overload for some reason and then raises an error that the type is wrong (it's not an array).

        return new SemanticMessage<object>( name, parameter.Name, parameter.Value );
    }

    /// <summary>
    /// Creates a semantic message with an arbitrary number of parameters.
    /// </summary>
    /// <param name="messageName">Message name.</param>
    /// <param name="parameters">Array of parameters (name-value pairs).</param>
    /// <returns></returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )] // To avoid copying the struct.
    public static SemanticMessageArray Semantic( string messageName, IReadOnlyList<(string Name, object? Value)> parameters ) => new( messageName, parameters );

    /// <summary>
    /// Creates a semantic message without parameter.
    /// </summary>
    /// <param name="messageName">Message name.</param>
    /// <returns></returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )] // To avoid copying the struct.
    public static SemanticMessage Semantic( string messageName ) => new( messageName );
}