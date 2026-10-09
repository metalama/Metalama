// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Read-only list of <see cref="ITypeParameterBuilder"/>, returned by the <c>TypeParameters</c> property of a builder.
    /// </summary>
    /// <seealso cref="ITypeParameterBuilder"/>
    /// <seealso cref="IMethodBuilder"/>
    /// <seealso href="@introducing-members"/>
    public interface ITypeParameterBuilderList : IReadOnlyList<ITypeParameterBuilder>
    {
        // This type does not extend ITypeParameterList, for the same reason as IParameterBuilderList: the indexer and GetEnumerator would be
        // ambiguous.

        /// <summary>
        /// Gets the type parameter with the specified name.
        /// </summary>
        /// <param name="name">The name of the type parameter.</param>
        /// <returns>The type parameter with the specified name.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">The list has no type parameter of this name.</exception>
        ITypeParameterBuilder this[ string name ] { get; }
    }
}
