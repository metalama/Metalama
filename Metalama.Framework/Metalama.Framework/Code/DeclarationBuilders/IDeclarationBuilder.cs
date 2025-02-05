// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Allows to complete the construction of a declaration that has been created by an advice.
    /// </summary>
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
        void AddAttribute( AttributeConstruction attribute );

        // TODO: There is no way to provide the value of an enum when the enum type is run-time-only.

        /// <summary>
        /// Removes all custom attributes of a given type from the current declaration.
        /// </summary>
        /// <param name="type">TYpe of custom attributes to be removed.</param>
        void RemoveAttributes( INamedType type );
    }
}