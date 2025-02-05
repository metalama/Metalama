// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code;

/// <summary>
/// A base interface for <see cref="IProperty"/>, <see cref="IField"/> and <see cref="IIndexer"/>.
/// </summary>
public interface IFieldOrPropertyOrIndexer : IHasAccessors
{
    /// <summary>
    /// Gets writeability of the field, property or indexer, i.e. the situations in which it can be written.
    /// </summary>
    Writeability Writeability { get; }

    /// <summary>
    /// Gets the property getter, or <c>null</c> if the property is write-only. In case of automatic properties, this property returns
    /// an object that does not map to source code but allows to add aspects and advice as with a normal method. In case of fields,
    /// this property returns a pseudo-method that can be the target of aspects and advice, as if the field were a property.
    /// </summary>
    IMethod? GetMethod { get; }

    /// <summary>
    /// Gets the property getter, or <c>null</c> if the property is read-only. In case of automatic properties, this property returns
    /// an object that does not map to source code but allows to add aspects and advice as with a normal method. In case of fields,
    /// this property returns a pseudo-method that can be the target of aspects and advice, as if the field were a property.
    /// </summary>
    IMethod? SetMethod { get; }

    new IRef<IFieldOrPropertyOrIndexer> ToRef();
}