// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Base interface for <see cref="IPropertyBuilder"/> and <see cref="IIndexerBuilder"/>.
/// </summary>
public interface IPropertyOrIndexerBuilder : IPropertyOrIndexer, IFieldOrPropertyOrIndexerBuilder
{
    /// <summary>
    /// Gets the <see cref="IMethodBuilder"/> for the getter.
    /// </summary>
    new IMethodBuilder? GetMethod { get; }

    /// <summary>
    /// Gets the <see cref="IMethodBuilder"/> for the setter.
    /// </summary>
    new IMethodBuilder? SetMethod { get; }
}