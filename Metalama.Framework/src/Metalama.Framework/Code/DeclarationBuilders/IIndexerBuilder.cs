// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of an indexer that has been created by an advice.
/// </summary>
/// <seealso cref="IIndexer"/>
/// <seealso cref="IPropertyOrIndexerBuilder"/>
/// <seealso cref="AdviserExtensions.IntroduceIndexer"/>
public interface IIndexerBuilder : IPropertyOrIndexerBuilder, IIndexer, IHasParametersBuilder
{
    /// <summary>
    /// Adds a parameter to the current indexer and specifies its type using an <see cref="IType"/>.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="refKind">The ref kind of the parameter (e.g., <c>ref</c>, <c>out</c>, <c>in</c>).</param>
    /// <param name="defaultValue">The default value of the parameter, or <c>null</c> if the parameter has no default value.</param>
    /// <returns>An <see cref="IParameterBuilder"/> that allows further configuration of the new parameter.</returns>
    IParameterBuilder AddParameter( string name, IType type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = default );

    /// <summary>
    /// Adds a parameter to the current indexer and specifies its type using a reflection <see cref="Type"/>.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="refKind">The ref kind of the parameter (e.g., <c>ref</c>, <c>out</c>, <c>in</c>).</param>
    /// <param name="defaultValue">The default value of the parameter, or <c>null</c> if the parameter has no default value.</param>
    /// <returns>An <see cref="IParameterBuilder"/> that allows further configuration of the new parameter.</returns>
    IParameterBuilder AddParameter( string name, Type type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = default );
}