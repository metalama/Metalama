// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Invokers;
using System.Collections.Generic;

namespace Metalama.Framework.Code;

/// <summary>
/// Represents an indexer, i.e. a <c>this[*]</c> property.
/// </summary>
/// <seealso cref="IProperty"/>
public interface IIndexer : IPropertyOrIndexer, IHasParameters, IIndexerInvoker
{
    /// <summary>
    /// Gets a list of interface properties this property explicitly implements.
    /// </summary>
    IReadOnlyList<IIndexer> ExplicitInterfaceImplementations { get; }

    /// <summary>
    /// Gets the base property that is overridden by the current property.
    /// </summary>
    IIndexer? OverriddenIndexer { get; }

    /// <summary>
    /// Gets the definition of the indexer. If the current declaration is an indexer of
    /// a generic type instance, this returns the indexer in the generic type definition. Otherwise, it returns the current instance.
    /// </summary>
    new IIndexer Definition { get; }

    new IRef<IIndexer> ToRef();
}