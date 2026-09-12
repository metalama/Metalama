// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of a delegate that has been created by an advice.
/// </summary>
/// <remarks>
/// <para>
/// This interface derives from <see cref="IMemberOrNamedTypeBuilder"/> and declares the members of a signature
/// itself. A delegate declaration is a method signature with the <c>delegate</c> keyword in front of it, so the
/// return type, the return parameter and the parameters describe the <c>Invoke</c> method that the compiler
/// synthesizes, while the name, the accessibility, the custom attributes and the type parameters describe the
/// delegate type.
/// </para>
/// <para>
/// The following inherited properties are not valid for a delegate, and their setter throws a
/// <see cref="NotSupportedException"/>: <see cref="IMemberOrNamedTypeBuilder.IsStatic"/>,
/// <see cref="IMemberOrNamedTypeBuilder.IsSealed"/>, <see cref="IMemberOrNamedTypeBuilder.IsAbstract"/> and
/// <see cref="IMemberOrNamedTypeBuilder.IsPartial"/>. A delegate is implicitly sealed, and it is never static,
/// abstract or partial.
/// </para>
/// <para>
/// A delegate builder is not an <see cref="INamedType"/>, so it may not be used where an <see cref="IType"/> is
/// expected. The introduced delegate is read from
/// <see cref="Metalama.Framework.Advising.IIntroductionAdviceResult{T}.Declaration"/>, which is how an aspect gives
/// it as the type of a field, of a property or of an event.
/// </para>
/// <para>
/// The <c>BeginInvoke</c> and <c>EndInvoke</c> methods are neither exposed nor built. They exist only for a
/// delegate compiled for .NET Framework, and they are the asynchronous pattern that preceded <c>async</c>.
/// </para>
/// </remarks>
/// <seealso cref="Metalama.Framework.Advising.IAdviceFactory.IntroduceDelegate"/>
/// <seealso cref="Metalama.Framework.Aspects.AdviserExtensions.IntroduceDelegate"/>
/// <seealso cref="Metalama.Framework.Code.Types.IDelegateFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IDelegateBuilder : IMemberOrNamedTypeBuilder
{
    /// <summary>
    /// Gets or sets the return type of the delegate. The default value is <c>void</c>.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IDelegateFacet.ReturnType"/>
    IType ReturnType { get; set; }

    /// <summary>
    /// Gets the return parameter of the delegate, which carries the reference kind of a <c>ref</c> return and the
    /// custom attributes of the return value.
    /// </summary>
    IParameterBuilder ReturnParameter { get; }

    /// <summary>
    /// Gets the parameters of the delegate, in the order in which they were added.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IDelegateFacet.Parameters"/>
    IParameterBuilderList Parameters { get; }

    /// <summary>
    /// Gets the type parameters of the delegate, in the order in which they were added.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The type parameters belong to the delegate type and not to its <c>Invoke</c> method, which is what the
    /// language declares: <c>Func&lt;T, TResult&gt;</c> is a generic type whose <c>Invoke</c> method is not
    /// generic.
    /// </para>
    /// </remarks>
    ITypeParameterList TypeParameters { get; }

    /// <summary>
    /// Appends a parameter to the delegate.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="refKind">The reference kind of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter, or <c>null</c> if the parameter has none.</param>
    /// <returns>An <see cref="IParameterBuilder"/> that allows you to complete the construction of the parameter.</returns>
    IParameterBuilder AddParameter( string name, IType type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = default );

    /// <inheritdoc cref="AddParameter(string,IType,RefKind,TypedConstant?)"/>
    IParameterBuilder AddParameter( string name, Type type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = null );

    /// <summary>
    /// Appends a type parameter to the delegate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A delegate is, with an interface, one of the two kinds of type whose type parameters may declare variance,
    /// so <see cref="ITypeParameterBuilder.Variance"/> is meaningful on the value this method returns.
    /// </para>
    /// </remarks>
    /// <param name="name">The name of the type parameter.</param>
    /// <returns>An <see cref="ITypeParameterBuilder"/> that allows you to complete the construction of the type parameter.</returns>
    ITypeParameterBuilder AddTypeParameter( string name );
}
