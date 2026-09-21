// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    /// <summary>
    /// Returns the <see cref="SerializableDeclarationId"/> of the declaration a reference points to, unless that declaration
    /// has none, in which case the method returns <c>false</c>. This is the non-throwing form of
    /// <see cref="IRef.ToSerializableId"/>.
    /// </summary>
    /// <remarks>
    /// A declaration of a file-local type has no identifier, because a declaration identifier names a type by its namespace
    /// and its name only, and two file-local types can share both. See issues #2051 and #662.
    /// </remarks>
    public static bool TryGetSerializableId( this IRef? reference, out SerializableDeclarationId id )
    {
        if ( reference is null )
        {
            id = default;

            return false;
        }

        return ((IRefImpl) reference).TryGetSerializableId( out id );
    }
}
