// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Collections
{
    /// <summary>
    /// Read-only list of <see cref="IAttribute"/>.
    /// </summary>
    /// <remarks>
    ///  <para>The order of items in this list is undetermined and may change between versions.</para>
    /// </remarks>
    public interface IAttributeCollection : IReadOnlyCollection<IAttribute>
    {
        IEnumerable<IAttribute> OfAttributeType( IType type );

        IEnumerable<IAttribute> OfAttributeType( IType type, ConversionKind conversionKind );

        IEnumerable<IAttribute> OfAttributeType( Type type );

        IEnumerable<IAttribute> OfAttributeType( Type type, ConversionKind conversionKind );

        IEnumerable<IAttribute> OfAttributeType( Func<IType, bool> predicate );

        /// <summary>
        /// Gets the constructed attributes of a given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IEnumerable<T> GetConstructedAttributesOfType<T>()
            where T : Attribute;

        bool Any( IType type );

        bool Any( IType type, ConversionKind conversionKind );

        bool Any( Type type );

        bool Any( Type type, ConversionKind conversionKind );
    }
}