// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Exposes a settable <see cref="Type"/> property.
/// </summary>
public interface IHasTypeBuilder : IHasType
{
    /// <summary>
    /// Gets or sets the type of the field or property.
    /// </summary>
    new IType Type { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Metalama.Framework.Code.RefKind"/> of the property, indexer or property
    /// (i.e. <see cref="Code.RefKind.Ref"/>, <see cref="Code.RefKind.Out"/>, ...).
    /// </summary>
    new RefKind RefKind { get; set; }
}