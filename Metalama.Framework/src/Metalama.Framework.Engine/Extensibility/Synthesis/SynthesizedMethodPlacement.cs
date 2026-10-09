// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Describes the type in which <see cref="CallSites.ExtensionTransformationFactory.DeclareMethod"/> declares a method.
/// </summary>
[PublicAPI]
public sealed class SynthesizedMethodPlacement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SynthesizedMethodPlacement"/> class. Use the static members of the class to create an instance.
    /// </summary>
    private SynthesizedMethodPlacement( INamedType? type, SynthesizedTypeHandle? synthesizedType )
    {
        this.Type = type;
        this.SynthesizedType = synthesizedType;
    }

    /// <summary>
    /// Creates a placement in a type of the compilation, which is a class, a struct or a record, declared in source code or introduced by an
    /// aspect. A generic type is given as its definition, and the declared method can use its type parameters.
    /// </summary>
    /// <param name="type">The type.</param>
    public static SynthesizedMethodPlacement InType( INamedType type ) => new( type ?? throw new ArgumentNullException( nameof(type) ), null );

    /// <summary>
    /// Creates a placement in a static class declared with <see cref="CallSites.ExtensionTransformationFactory.DeclareStaticClass"/>.
    /// </summary>
    /// <param name="type">The handle of the static class.</param>
    public static SynthesizedMethodPlacement InType( SynthesizedTypeHandle type )
        => new( null, type ?? throw new ArgumentNullException( nameof(type) ) );

    /// <summary>
    /// Gets the type of the compilation, or <c>null</c> when the placement is a declared static class.
    /// </summary>
    public INamedType? Type { get; }

    /// <summary>
    /// Gets the declared static class, or <c>null</c> when the placement is a type of the compilation.
    /// </summary>
    public SynthesizedTypeHandle? SynthesizedType { get; }
}
