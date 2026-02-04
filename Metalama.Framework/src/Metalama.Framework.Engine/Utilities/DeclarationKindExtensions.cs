// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.Utilities;

#if ROSLYN_5_0_0_OR_GREATER

/// <summary>
/// Extension properties for <see cref="DeclarationKind"/>.
/// </summary>
internal static class DeclarationKindExtensions
{
    extension( DeclarationKind kind )
    {
        /// <summary>
        /// Gets a value indicating whether the declaration kind represents a member
        /// (method, constructor, property, indexer, field, or event).
        /// </summary>
        public bool IsMember
            => kind is DeclarationKind.Method or DeclarationKind.Constructor or DeclarationKind.Property
                or DeclarationKind.Indexer or DeclarationKind.Field or DeclarationKind.Event;

        /// <summary>
        /// Gets a value indicating whether the declaration kind represents a member or named type
        /// (method, constructor, property, indexer, field, event, named type, or extension block).
        /// </summary>
        public bool IsMemberOrNamedType
            => kind is DeclarationKind.Method or DeclarationKind.Constructor or DeclarationKind.Property
                or DeclarationKind.Indexer or DeclarationKind.Field or DeclarationKind.Event
                or DeclarationKind.NamedType or DeclarationKind.ExtensionBlock;
    }
}

#else

/// <summary>
/// Extension methods for <see cref="DeclarationKind"/>.
/// </summary>
internal static class DeclarationKindExtensions
{
    /// <summary>
    /// Determines whether the declaration kind represents a member
    /// (method, constructor, property, indexer, field, or event).
    /// </summary>
    public static bool IsMember( this DeclarationKind kind )
        => kind is DeclarationKind.Method or DeclarationKind.Constructor or DeclarationKind.Property
            or DeclarationKind.Indexer or DeclarationKind.Field or DeclarationKind.Event;

    /// <summary>
    /// Determines whether the declaration kind represents a member or named type
    /// (method, constructor, property, indexer, field, event, named type, or extension block).
    /// </summary>
    public static bool IsMemberOrNamedType( this DeclarationKind kind )
        => kind is DeclarationKind.Method or DeclarationKind.Constructor or DeclarationKind.Property
            or DeclarationKind.Indexer or DeclarationKind.Field or DeclarationKind.Event
            or DeclarationKind.NamedType or DeclarationKind.ExtensionBlock;
}

#endif
