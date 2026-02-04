// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Extension properties for <see cref="TypeKind"/>.
/// </summary>
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
    }
}
