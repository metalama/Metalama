// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Allows to complete the construction of a method that has been created by the <see cref="Metalama.Framework.Aspects.AdviserExtensions.IntroduceMethod"/> advice.
    /// </summary>
    /// <seealso cref="IMethod"/>
    /// <seealso cref="IMethodBaseBuilder"/>
    /// <seealso cref="AdviserExtensions.IntroduceMethod(IAdviser{INamedType}, string, IntroductionScope, OverrideStrategy, System.Action{IMethodBuilder}?, object?, object?)"/>
    /// <seealso href="@introducing-members"/>
    public interface IMethodBuilder : IMethod, IMethodBaseBuilder
    {
        /// <summary>
        /// Adds a generic parameter to the method.
        /// </summary>
        /// <param name="name">The name of the generic type parameter to add.</param>
        /// <returns>An <see cref="ITypeParameterBuilder"/> that allows you to further configure the new type parameter, including constraints and variance.</returns>
        ITypeParameterBuilder AddTypeParameter( string name );

        /// <summary>
        /// Gets the type parameters of the method, as builders that can be changed.
        /// </summary>
        new ITypeParameterBuilderList TypeParameters { get; }

        /// <summary>
        /// Adds a type parameter that copies an existing type parameter: its name, its variance, its kind constraints and its type constraints.
        /// </summary>
        /// <param name="prototype">The type parameter to copy, typically a type parameter of another method or type.</param>
        /// <param name="includeCustomAttributes">A value indicating whether the custom attributes of <paramref name="prototype"/> are copied.</param>
        /// <returns>An <see cref="ITypeParameterBuilder"/> that allows you to change the copy.</returns>
        /// <remarks>
        /// <para>
        /// The builder records the copy. In the type constraints of the copy, and in the types of the type parameters and parameters that are
        /// copied afterwards, a reference to <paramref name="prototype"/> is replaced by a reference to the copy. Copy the type parameters
        /// before the parameters that use them.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">A type parameter of the same name already exists.</exception>
        ITypeParameterBuilder AddTypeParameter( ITypeParameter prototype, bool includeCustomAttributes = false );

        /// <summary>
        /// Gets an object allowing to read and modify the method return type and custom attributes,
        /// or <c>null</c> for methods that don't have return types: constructors and finalizers.
        /// </summary>
        new IParameterBuilder ReturnParameter { get; }

        /// <summary>
        /// Gets or sets the method return type.
        /// </summary>
        new IType ReturnType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method is read-only (applicable to struct methods).
        /// </summary>
        new bool IsReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the operator kind. When set to a value other than <see cref="OperatorKind.None"/>,
        /// the method becomes an operator. The name and <see cref="IMemberOrNamedType.IsStatic"/>
        /// properties are automatically set based on the operator kind. This property can only be set once.
        /// </summary>
        new OperatorKind OperatorKind { get; set; }
    }
}