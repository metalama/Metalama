// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Extension properties for <see cref="TypeKind"/>.
/// </summary>
[PublicAPI]
public static class TypeKindExtensions
{
    extension( TypeKind kind )
    {
        /// <summary>
        /// Gets a value indicating whether the type kind represents a named type
        /// (class, struct, interface, enum, or delegate).
        /// </summary>
        public bool IsNamedType
            => kind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface
                or TypeKind.Enum or TypeKind.Delegate;

        /// <summary>
        /// Gets a value indicating whether the type kind represents a class or struct.
        /// </summary>
        public bool IsClassOrStruct => kind is TypeKind.Class or TypeKind.Struct;

        /// <summary>
        /// Gets a value indicating whether the type kind represents a named type or an extension block, which
        /// together are the kinds that an aspect can introduce and that <c>NamedTypeBuilder</c> represents.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The property above does not include <see cref="TypeKind.Extension"/>, and must not: the visitors of the
        /// code model test it before they test the extension block, so an extension block reported as a named type
        /// would be visited as one. A member declared in an extension block cannot be the target of a cref, which is
        /// why it is named here in prose.
        /// </para>
        /// </remarks>
        public bool IsNamedTypeOrExtension => kind.IsNamedType || kind is TypeKind.Extension;
    }
}