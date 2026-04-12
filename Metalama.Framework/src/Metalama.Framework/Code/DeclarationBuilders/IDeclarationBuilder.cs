// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using System.Collections.Generic;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Allows to complete the construction of a declaration that has been created by an advice.
    /// </summary>
    /// <seealso cref="IDeclaration"/>
    /// <seealso cref="IIntroductionAdviceResult{T}"/>
    /// <seealso cref="AttributeConstruction"/>
    /// <seealso href="@introducing-members"/>
    public interface IDeclarationBuilder : IDeclaration
    {
        /// <summary>
        /// Gets a value indicating whether the builder has been frozen. When the value is <c>true</c>, modifications can no longer be performed.
        /// </summary>
        bool IsFrozen { get; }

        /// <summary>
        /// Freezes the declaration so that modifications can no longer be performed.
        /// </summary>
        void Freeze();

        /// <summary>
        /// Adds a custom attribute to the current declaration.
        /// </summary>
        /// <param name="attribute">The attribute to add.</param>
        void AddAttribute( AttributeConstruction attribute );

        // TODO: There is no way to provide the value of an enum when the enum type is run-time-only.

        /// <summary>
        /// Adds custom attributes to the current declaration.
        /// </summary>
        /// <param name="attributes">The attribute constructions to add.</param>
        void AddAttributes( IEnumerable<AttributeConstruction> attributes );

        /// <summary>
        /// Adds custom attributes to the current declaration by copying them from existing <see cref="IAttribute"/> instances.
        /// This is useful when replacing or introducing a declaration and copying the attributes from the original declaration.
        /// Callers can filter the sequence (e.g. using LINQ <c>Where</c>) before passing it.
        /// </summary>
        /// <param name="attributes">The attributes to copy. Each attribute is converted to an <see cref="AttributeConstruction"/>
        /// via <see cref="AttributeExtensions.ToAttributeConstruction"/>.</param>
        void AddAttributes( IEnumerable<IAttribute> attributes );

        /// <summary>
        /// Removes all custom attributes of a given type from the current declaration.
        /// </summary>
        /// <param name="type">Type of custom attributes to be removed.</param>
        void RemoveAttributes( INamedType type );
    }
}