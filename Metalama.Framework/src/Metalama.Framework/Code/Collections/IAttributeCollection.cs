// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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