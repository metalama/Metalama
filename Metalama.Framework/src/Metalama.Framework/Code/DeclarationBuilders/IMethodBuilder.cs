// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Allows to complete the construction of a method that has been created by an advice.
    /// </summary>
    /// <seealso cref="IMethod"/>
    /// <seealso cref="IMethodBaseBuilder"/>
    /// <seealso cref="AdviserExtensions.IntroduceMethod(IAdviser{INamedType}, string, IntroductionScope, OverrideStrategy, System.Action{IMethodBuilder}?, object?, object?)"/>
    /// <seealso href="@introducing-members"/>
    public interface IMethodBuilder : IMethod, IMethodBaseBuilder
    {
        // TODO: Add an overload for adding generic parameter which would initialize it with values for covariance/contravariance and constraints.

        /// <summary>
        /// Adds a generic parameter to the method.
        /// </summary>
        /// <param name="name">The name of the generic type parameter to add.</param>
        /// <returns>An <see cref="ITypeParameterBuilder"/> that allows you to further configure the new type parameter, including constraints and variance.</returns>
        ITypeParameterBuilder AddTypeParameter( string name );

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