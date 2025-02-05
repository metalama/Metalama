// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of an indexer that has been created by an advice.
/// </summary>
public interface IIndexerBuilder : IPropertyOrIndexerBuilder, IIndexer, IHasParametersBuilder
{
    /// <summary>
    /// Adds a parameter to the current indexer and specifies its type using an <see cref="IType"/>.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <param name="refKind"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    IParameterBuilder AddParameter( string name, IType type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = default );

    /// <summary>
    /// Adds a parameter to the current indexer and specifies its type using a reflection <see cref="Type"/>.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <param name="refKind"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    IParameterBuilder AddParameter( string name, Type type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = default );
}