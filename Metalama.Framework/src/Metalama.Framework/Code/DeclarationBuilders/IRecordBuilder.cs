// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of a record that has been created by the <see cref="Metalama.Framework.Aspects.AdviserExtensions.IntroduceRecord"/> advice. One interface serves a
/// record class and a record struct.
/// </summary>
/// <remarks>
/// <para>
/// The authoring form is chosen when the advice is called and is reported by <see cref="RecordKind"/>.
/// </para>
/// <para>
/// The members that the compiler synthesizes for a record are not set through this interface. They are created by
/// the advice and are read through <see cref="INamedType.Facets"/> on the introduced type, as they are for a record
/// that the user wrote.
/// </para>
/// </remarks>
/// <seealso cref="Metalama.Framework.Code.Types.IRecordFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IRecordBuilder : INamedTypeBuilder
{
    /// <summary>
    /// Gets the authoring form of the record, which was chosen when the advice was called.
    /// </summary>
    RecordKind RecordKind { get; }

    /// <summary>
    /// Gets the positional parameters that have been added so far, in the order in which they were added.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IRecordFacet.PositionalProperties"/>
    IParameterBuilderList PositionalParameters { get; }

    /// <summary>
    /// Appends a positional parameter to the record, which declares a parameter of the primary constructor and a
    /// public init-only property of the same name and type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A record that has at least one positional parameter is a positional record, and the compiler synthesizes a
    /// <c>Deconstruct</c> method for it. A record that has none declares no parameter list, and
    /// <see cref="Metalama.Framework.Code.Types.IRecordFacet.DeconstructMethod"/> is then <c>null</c> on the
    /// introduced type.
    /// </para>
    /// </remarks>
    /// <param name="name">The name of the parameter, which is also the name of the property it declares.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter, or <c>null</c> when it has none.</param>
    /// <returns>An <see cref="IParameterBuilder"/> that allows you to further build the new parameter, in particular
    ///     to add custom attributes to it.</returns>
    IParameterBuilder AddPositionalParameter( string name, IType type, TypedConstant? defaultValue = default );

    /// <inheritdoc cref="AddPositionalParameter(string,IType,TypedConstant?)"/>
    IParameterBuilder AddPositionalParameter( string name, Type type, TypedConstant? defaultValue = null );

    /// <summary>
    /// Adds an argument that the record passes to the primary constructor of its base record, which the language
    /// writes in the base list of the declaration, as in <c>record Derived( int X ) : BaseRecord( X )</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A record class that derives from a record whose only constructor is a primary constructor has to pass
    /// arguments to it, and the compiler reports CS1729 on the generated declaration when it does not.
    /// </para>
    /// <para>
    /// Set <see cref="INamedTypeBuilder.BaseType"/> before calling this method. The method throws a
    /// <see cref="NotSupportedException"/> on a record struct, which has no base list, and an
    /// <see cref="InvalidOperationException"/> when the base type is still <see cref="object"/>.
    /// </para>
    /// </remarks>
    /// <param name="argument">The expression to pass as an argument. A positional parameter of the record is
    ///     referenced through the <see cref="IParameterBuilder"/> that <c>AddPositionalParameter</c> returns.</param>
    /// <param name="parameterName">The optional name of the parameter of the base constructor to which the argument
    ///     is assigned. When <c>null</c>, the arguments are assigned positionally.</param>
    void AddBaseArgument( IExpression argument, string? parameterName = null );
}
