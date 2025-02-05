// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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