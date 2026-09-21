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
    /// A reference that cannot be represented by an identifier at all, such as a reference to an attribute, reports that
    /// fact through the <c>false</c> result rather than by an exception. See the documentation of
    /// <see cref="IRef.ToSerializableId"/> and issue #2051.
    /// </remarks>
    public static bool TryGetSerializableId( this IRef? reference, out SerializableDeclarationId id )
    {
        if ( reference == null )
        {
            id = default;

            return false;
        }

        return ((ISdkRef) reference).TryGetSerializableId( out id );
    }
}
