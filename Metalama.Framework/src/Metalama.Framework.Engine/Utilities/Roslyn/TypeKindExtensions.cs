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

        /// <summary>
        /// Gets the name of the type kind as it is written in a message addressed to an aspect author, optionally
        /// preceded by the indefinite article that the name takes.
        /// </summary>
        /// <param name="addArticle">
        /// <c>true</c> to prefix the name with <c>a</c> or <c>an</c>, <c>false</c> to return the name alone.
        /// </param>
        public string GetDisplayName( bool addArticle = false )
        {
            var name = kind switch
            {
                TypeKind.Array => "array",
                TypeKind.Class => "class",
                TypeKind.Delegate => "delegate",
                TypeKind.Dynamic => "dynamic type",
                TypeKind.Enum => "enum",
                TypeKind.TypeParameter => "type parameter",
                TypeKind.Interface => "interface",
                TypeKind.Pointer => "pointer",
                TypeKind.Struct => "struct",
                TypeKind.FunctionPointer => "function pointer",
                TypeKind.Error => "error type",
                TypeKind.Extension => "extension block",
                _ => kind.ToString()
            };

            if ( !addArticle )
            {
                return name;
            }

            // A record is TypeKind.Class or TypeKind.Struct with INamedType.IsRecord, so the obsolete record kinds are
            // not named here and fall to the default arm.
            // The article is decided by the first letter of the name, which is enough for the names above: none of
            // them begins with a vowel that takes "a", and none begins with a consonant that takes "an".
            return (name[0] is 'a' or 'e' or 'i' or 'o' or 'u' ? "an " : "a ") + name;
        }
    }
}