// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// The non-generic members that <see cref="BaseRef{T}"/> adds to <see cref="IRef"/>.
/// </summary>
internal interface IRefImpl : IRef
{
    /// <summary>
    /// Returns the <see cref="SerializableDeclarationId"/> of the referenced declaration, unless that declaration has none,
    /// in which case the method returns <c>false</c>. This is the non-throwing form of <see cref="IRef.ToSerializableId"/>.
    /// </summary>
    bool TryGetSerializableId( out SerializableDeclarationId id );
}
