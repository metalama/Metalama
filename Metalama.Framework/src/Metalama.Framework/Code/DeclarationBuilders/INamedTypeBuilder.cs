// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.DeclarationBuilders;

public interface INamedTypeBuilder : IMemberOrNamedTypeBuilder, INamedType
{
    /// <summary>
    /// Gets or sets a value indicating whether the type is marked as <c>partial</c> in source code. 
    /// </summary>
    new bool IsPartial { get; set; }

    // TODO: Struct introduction

    ///// <summary>
    ///// Gets or sets a value indicating whether the type is <c>readonly</c>.
    ///// </summary>
    // new bool IsReadOnly { get; set; }

    ///// <summary>
    ///// Gets or sets a value indicating whether the type is a <c>ref</c> struct.
    ///// </summary>
    // new bool IsRef { get; set; }

    /// <summary>
    /// Gets or sets the type from which the current type derives.
    /// </summary>
    new INamedType? BaseType { get; set; }

    // TODO: Primary constructor handling.

    ///// <summary>
    ///// Gets the primary constructor builder if it is defined, otherwise returns <c>null</c>.
    ///// </summary>
    // new IConstructorBuilder? PrimaryConstructor { get; }

    /// <summary>
    /// Adds a generic parameter to the type.
    /// </summary>
    /// <param name="name">Name of the generic parameter.</param>
    /// <returns>An <see cref="ITypeParameterBuilder"/> that allows you to further build the new parameter.</returns>
    ITypeParameterBuilder AddTypeParameter( string name );
}