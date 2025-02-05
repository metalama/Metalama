// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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