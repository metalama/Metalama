// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising.OverloadingStrategies;

namespace Metalama.Framework.Advising;

/// <summary>
/// Provides factory access to standard implementations of the <see cref="IConstructorOverloadingStrategy"/> interface.
/// </summary>
/// <seealso cref="IConstructorOverloadingStrategy"/>
public static class ConstructorOverloadingStrategy
{
    /// <summary>
    /// Gets a strategy that generates a forwarding constructor for every source-origin constructor that the
    /// framework mutates. Aspect-introduced constructors are not themselves overloaded.
    /// </summary>
    public static IConstructorOverloadingStrategy ForwardSourceConstructors { get; } = new ForwardSourceConstructorsStrategy();

    /// <summary>
    /// Gets a strategy that generates a forwarding constructor only when the mutated constructor is the
    /// parameterless source-origin constructor. This is the typical choice for types that must remain
    /// constructible via <see cref="System.Activator.CreateInstance{T}()"/> or a <c>new()</c> generic constraint
    /// while the aspect enriches the constructor signature with additional parameters.
    /// </summary>
    public static IConstructorOverloadingStrategy ForwardDefaultConstructor { get; } = new ForwardDefaultConstructorStrategy();
}
