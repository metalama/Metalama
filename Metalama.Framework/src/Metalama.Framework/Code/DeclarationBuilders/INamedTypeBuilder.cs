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
    /// Metalama runs inside a host, which is the compiler during a build and the integrated development environment
    /// at design time, and the host supplies the version of Roslyn that Metalama uses. Setting this property to
    /// <c>true</c> throws an <see cref="InvalidOperationException"/> when that version does not offer C# 15, because
    /// the <c>closed</c> keyword cannot be generated then. A host that supplies such a version does not support the
    /// language feature at all, so it cannot compile a closed class either.
    /// </para>
    /// </remarks>
    new bool IsClosed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the type is declared with the <c>readonly</c> modifier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The language allows the <c>readonly</c> modifier on a struct only. The setter throws an
    /// <see cref="InvalidOperationException"/> when the type being built is not a struct.
    /// </para>
    /// <para>
    /// A readonly struct may declare no settable instance field and no automatic property that has a setter.
    /// Metalama does not refuse an advice that introduces one, and the compiler reports the error on the generated
    /// declaration.
    /// </para>
    /// </remarks>
    new bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the type is declared with the <c>ref</c> modifier, which makes it a
    /// type that may live on the stack only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The language allows the <c>ref</c> modifier on a struct that is not a record. The setter throws an
    /// <see cref="InvalidOperationException"/> in any other case.
    /// </para>
    /// <para>
    /// A ref struct may not be used as a type argument, may not be a field of a type that is not itself a ref
    /// struct, and may not be boxed. Metalama does not verify those restrictions on the code that an aspect
    /// generates, so the compiler reports them on the generated code.
    /// </para>
    /// </remarks>
    new bool IsRef { get; set; }

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