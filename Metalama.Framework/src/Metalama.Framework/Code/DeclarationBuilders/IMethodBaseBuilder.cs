// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    public interface IMethodBaseBuilder : IMethodBase, IHasParametersBuilder
    {
        /// <summary>
        /// Appends a parameter to the method.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="type">Parameter type.</param>
        /// <param name="refKind"><c>out</c>, <c>ref</c>...</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>A <see cref="IParameterBuilder"/> that allows you to further build the new parameter.</returns>
        IParameterBuilder AddParameter( string name, IType type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = default );

        /// <summary>
        /// Appends a parameter to the method.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="type">Parameter type.</param>
        /// <param name="refKind"><c>out</c>, <c>ref</c>...</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>A <see cref="IParameterBuilder"/> that allows you to further build the new parameter.</returns>
        IParameterBuilder AddParameter( string name, Type type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = null );
    }
}