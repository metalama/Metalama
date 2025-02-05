// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Base interface for <see cref="IFieldBuilder"/>, <see cref="IPropertyBuilder"/> and <see cref="IIndexerBuilder"/>.
/// </summary>
public interface IFieldOrPropertyOrIndexerBuilder : IFieldOrPropertyOrIndexer, IMemberBuilder, IHasTypeBuilder
{
    /// <summary>
    /// Gets or sets writeability of the field, property or indexer, i.e. the situations in which it can be written.
    /// </summary>
    new Writeability Writeability { get; set; }
}