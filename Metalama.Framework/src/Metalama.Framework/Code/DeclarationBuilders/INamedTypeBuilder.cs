// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <seealso cref="INamedType"/>
/// <seealso cref="IMemberOrNamedTypeBuilder"/>
/// <seealso cref="AdviserExtensions.IntroduceClass(IAdviser{Metalama.Framework.Code.INamespaceOrNamedType}, string, OverrideStrategy, System.Action{Metalama.Framework.Code.DeclarationBuilders.INamedTypeBuilder}?)"/>
/// <seealso href="@introducing-types"/>
public interface INamedTypeBuilder : IMemberOrNamedTypeBuilder, INamedType
{
    /// <summary>
    /// Gets or sets a value indicating whether the type is marked as <c>partial</c> in source code.
    /// </summary>
    new bool IsPartial { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the type is declared with the <c>closed</c> modifier of C# 15.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The language allows the <c>closed</c> modifier on a class only, and forbids it on a sealed class and on a
    /// static class. The setter throws an <see cref="InvalidOperationException"/> when one of these restrictions is
    /// not met.
    /// </para>
    /// <para>
    /// A closed class is implicitly abstract, so <see cref="IMemberOrNamedTypeBuilder.IsAbstract"/> reports
    /// <c>true</c> as soon as this property is set, and the generated declaration carries the <c>closed</c> keyword
    /// instead of the <c>abstract</c> keyword. Setting <see cref="IMemberOrNamedTypeBuilder.IsAbstract"/> to
    /// <c>false</c> on a closed type throws an <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// The keyword is emitted by a Metalama engine that uses a Roslyn version that offers C# 15. Setting this
    /// property to <c>true</c> throws an <see cref="InvalidOperationException"/> under any other Roslyn version,
    /// because such an engine cannot generate a closed class. A host that presents such a Roslyn version cannot
    /// compile a closed hierarchy either.
    /// </para>
    /// </remarks>
    new bool IsClosed { get; set; }

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