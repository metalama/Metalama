// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Introspection.Navigation.Links;

namespace Metalama.Introspection.Navigation.Results;

/// <summary>
/// A node of a result tree: a value summarized for display, independently of the output format.
/// </summary>
internal abstract class ResultNode
{
    /// <summary>
    /// Gets the link that resolves the value again, or <c>null</c> when the value has none.
    /// </summary>
    public Link? Link { get; init; }
}
