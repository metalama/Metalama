// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.References
{
    internal interface ISdkRef : IRef
    {
        // This is a temporary method to extract the symbol from the reference, when there is any.
        // In the final implementation, this method should not be necessary.
        ISymbol? GetSymbol( Compilation compilation, bool ignoreAssemblyKey = false );

        /// <summary>
        /// Returns the <see cref="SerializableDeclarationId"/> of the referenced declaration, unless that declaration has
        /// none, in which case the method returns <c>false</c>. This is the non-throwing form of
        /// <see cref="IRef.ToSerializableId"/>.
        /// </summary>
        /// <remarks>
        /// This member belongs to the interface that every reference implementation implements, rather than to a narrower
        /// one, so that a reference which cannot be represented by an identifier at all, such as a reference to an
        /// attribute, reports that fact through the <c>false</c> result instead of failing a cast. See issue #2051.
        /// </remarks>
        bool TryGetSerializableId( out SerializableDeclarationId id );
    }
}