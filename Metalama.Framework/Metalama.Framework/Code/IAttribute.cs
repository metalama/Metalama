// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represent a custom attributes.
    /// </summary>
    /// <seealso cref="AttributeExtensions"/>
    public interface IAttribute : IDeclaration, IAttributeData, IAspectPredecessor
    {
        /// <summary>
        /// Gets the declaration that owns the custom attribute.
        /// </summary>
        new IDeclaration ContainingDeclaration { get; }

        new IRef<IAttribute> ToRef();
    }
}