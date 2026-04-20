// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Advising;

/// <summary>
/// Provides factory access to the standard implementations of the <see cref="IConstructorOverloadingStrategy"/>
/// interface. Each factory returns a <see cref="ForwardConstructorStrategy"/> whose
/// <see cref="ForwardConstructorStrategy.WithObsoleteAttribute"/> method can be used to additionally decorate
/// the generated forwarding constructor with <see cref="System.ObsoleteAttribute"/>.
/// </summary>
/// <seealso cref="IConstructorOverloadingStrategy"/>
/// <seealso cref="ForwardConstructorStrategy"/>
public static class ConstructorOverloadingStrategy
{
    /// <summary>
    /// Gets a strategy that generates a forwarding constructor for every source constructor
    /// that the framework mutates. Aspect-introduced constructors are not themselves overloaded. Call
    /// <see cref="ForwardConstructorStrategy.WithObsoleteAttribute"/> on the returned instance to additionally
    /// decorate the generated forwarding constructor with <see cref="System.ObsoleteAttribute"/>.
    /// </summary>
    public static ForwardConstructorStrategy ForwardSourceConstructors { get; } = new( defaultConstructorOnly: false );

    /// <summary>
    /// Gets a strategy that generates a forwarding constructor only when the mutated constructor is
    /// the parameterless constructor, if it exists. This is the typical choice for types that must remain
    /// constructible via <see cref="System.Activator.CreateInstance{T}()"/> or a <c>new()</c> generic constraint
    /// while the aspect enriches the constructor signature with additional parameters. Call
    /// <see cref="ForwardConstructorStrategy.WithObsoleteAttribute"/> on the returned instance to additionally
    /// decorate the generated forwarding constructor with <see cref="System.ObsoleteAttribute"/>.
    /// </summary>
    public static ForwardConstructorStrategy ForwardDefaultConstructor { get; } = new( defaultConstructorOnly: true );
}