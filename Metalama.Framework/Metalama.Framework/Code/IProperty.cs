// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Generic;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a property.
    /// </summary>
    /// <seealso cref="IIndexer"/>
    public interface IProperty : IFieldOrProperty, IPropertyOrIndexer
    {
        /// <summary>
        /// Gets a list of interface properties this property explicitly implements.
        /// </summary>
        IReadOnlyList<IProperty> ExplicitInterfaceImplementations { get; }

        /// <summary>
        /// Gets the base property that is overridden by the current property.
        /// </summary>
        IProperty? OverriddenProperty { get; }

        /// <summary>
        /// Gets the definition of the property. If the current declaration is a property of
        /// a generic type instance, this returns the property in the generic type definition. Otherwise, it returns the current instance.
        /// </summary>
        new IProperty Definition { get; }

        new IRef<IProperty> ToRef();

        /// <summary>
        /// Gets the <see cref="IField"/> from which the current property was generated. This property returns
        /// <c>null</c> in compilations in which the field has <i>already</i> been transformed into a property.
        /// It returns non-null only if the field is <i>being</i> transformed into a property.  The opposite side of this relationship is the
        /// <see cref="IField.OverridingProperty"/> of the <see cref="IProperty"/> interface.
        /// </summary>
        IField? OriginalField { get; }
    }
}