// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Allows to complete the construction of a method that has been created by an advice.
    /// </summary>
    public interface IMethodBuilder : IMethod, IMethodBaseBuilder
    {
        // TODO: Add an overload for adding generic parameter which would initialize it with values for covariance/contravariance and constraints.

        /// <summary>
        /// Adds a generic parameter to the method.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>An <see cref="ITypeParameterBuilder"/> that allows you to further build the new parameter.</returns>
        ITypeParameterBuilder AddTypeParameter( string name );

        /// <remarks>
        /// Gets an object allowing to read and modify the method return type and custom attributes,
        /// or  <c>null</c> for methods that don't have return types: constructors and finalizers.
        /// </remarks>
        new IParameterBuilder ReturnParameter { get; }

        /// <summary>
        /// Gets or sets the method return type.
        /// </summary>
        new IType ReturnType { get; set; }

        new bool IsReadOnly { get; set; }
    }
}