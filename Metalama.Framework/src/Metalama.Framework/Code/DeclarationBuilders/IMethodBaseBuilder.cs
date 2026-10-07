// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Base interface for <see cref="IMethodBuilder"/> and <see cref="IConstructorBuilder"/>.
    /// </summary>
    /// <seealso cref="IMethodBuilder"/>
    /// <seealso cref="IConstructorBuilder"/>
    /// <seealso cref="IMethodBase"/>
    /// <seealso cref="IParameterBuilder"/>
    /// <seealso href="@introducing-members"/>
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

        /// <summary>
        /// Appends a parameter that copies an existing parameter: its name, its type and its reference kind.
        /// </summary>
        /// <param name="prototype">The parameter to copy, typically a parameter of another method.</param>
        /// <param name="includeCustomAttributes">A value indicating whether the custom attributes of <paramref name="prototype"/> are copied.</param>
        /// <param name="includeDefaultValues">A value indicating whether the default value and the <c>params</c> modifier of
        /// <paramref name="prototype"/> are copied.</param>
        /// <returns>An <see cref="IParameterBuilder"/> that allows you to change the copy.</returns>
        /// <remarks>
        /// <para>
        /// In the type of the copy, a reference to a type parameter that was copied with <c>AddTypeParameter(ITypeParameter, bool)</c> is replaced
        /// by a reference to its copy. The <c>this</c> modifier is never copied.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">A parameter of the same name already exists.</exception>
        IParameterBuilder AddParameter( IParameter prototype, bool includeCustomAttributes = false, bool includeDefaultValues = false );

        /// <summary>
        /// Inserts a parameter at the specified index in the method's parameter list.
        /// </summary>
        /// <param name="index">The zero-based index at which the parameter should be inserted.</param>
        /// <param name="name">Parameter name.</param>
        /// <param name="type">Parameter type.</param>
        /// <param name="refKind"><c>out</c>, <c>ref</c>...</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>A <see cref="IParameterBuilder"/> that allows you to further build the new parameter.</returns>
        IParameterBuilder InsertParameter( int index, string name, IType type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = default );

        /// <summary>
        /// Inserts a parameter at the specified index in the method's parameter list.
        /// </summary>
        /// <param name="index">The zero-based index at which the parameter should be inserted.</param>
        /// <param name="name">Parameter name.</param>
        /// <param name="type">Parameter type.</param>
        /// <param name="refKind"><c>out</c>, <c>ref</c>...</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>A <see cref="IParameterBuilder"/> that allows you to further build the new parameter.</returns>
        IParameterBuilder InsertParameter( int index, string name, Type type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = null );
    }
}