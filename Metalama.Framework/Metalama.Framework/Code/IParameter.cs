// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Reflection;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a parameter of a method or property.
    /// </summary>
    public interface IParameter : INamedDeclaration, IExpression
    {
        /// <summary>
        /// Gets the parameter position, or <c>-1</c> for <see cref="IMethod.ReturnParameter"/>.
        /// </summary>
        int Index { get; }

        /// <remarks>
        /// Gets the default value of the parameter, or  <c>default</c> if the parameter type is a struct and the default
        /// value of the parameter is the default value of the struct type.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">The parameter has no default value.</exception>
        TypedConstant? DefaultValue { get; }

        /// <summary>
        /// Gets a value indicating whether the parameter has the <see langword="params" /> modifier.
        /// </summary>
        bool IsParams { get; }

        /// <summary>
        /// Gets a value indicating whether the parameter has the <see langword="this" /> modifier, meaning that the containing method is an extension method.
        /// </summary>
        bool IsThis { get; }

        /// <summary>
        /// Gets the parent <see cref="IMethod"/>, <see cref="IConstructor"/> or <see cref="IIndexer"/>.
        /// </summary>
        IHasParameters DeclaringMember { get; }

        /// <summary>
        /// Gets a <see cref="ParameterInfo"/> that represents the current parameter at run time.
        /// </summary>
        /// <returns>A <see cref="ParameterInfo"/> that can be used only in run-time code.</returns>
        ParameterInfo ToParameterInfo();

        bool IsReturnParameter { get; }

        new IRef<IParameter> ToRef();
    }
}