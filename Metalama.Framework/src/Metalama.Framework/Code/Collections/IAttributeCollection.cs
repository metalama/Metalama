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
    /// <seealso cref="IAttribute"/>
    /// <seealso cref="IDeclaration"/>
    public interface IAttributeCollection : IReadOnlyCollection<IAttribute>
    {
        /// <summary>
        /// Gets all attributes whose type is convertible to the specified <paramref name="type"/> using the default conversion kind.
        /// </summary>
        /// <param name="type">The attribute type to filter by.</param>
        /// <returns>An enumeration of attributes whose type is convertible to <paramref name="type"/>.</returns>
        /// <seealso cref="OfAttributeType(IType, ConversionKind)"/>
        IEnumerable<IAttribute> OfAttributeType( IType type );

        /// <summary>
        /// Gets all attributes whose type is convertible to the specified <paramref name="type"/> using the specified <paramref name="conversionKind"/>.
        /// </summary>
        /// <param name="type">The attribute type to filter by.</param>
        /// <param name="conversionKind">The kind of conversion to use when comparing types.</param>
        /// <returns>An enumeration of attributes whose type is convertible to <paramref name="type"/> according to <paramref name="conversionKind"/>.</returns>
        /// <seealso cref="ConversionKind"/>
        IEnumerable<IAttribute> OfAttributeType( IType type, ConversionKind conversionKind );

        /// <summary>
        /// Gets all attributes whose type is convertible to the specified reflection <paramref name="type"/> using the default conversion kind.
        /// </summary>
        /// <param name="type">The reflection attribute type to filter by.</param>
        /// <returns>An enumeration of attributes whose type is convertible to <paramref name="type"/>.</returns>
        /// <seealso cref="OfAttributeType(Type, ConversionKind)"/>
        IEnumerable<IAttribute> OfAttributeType( Type type );

        /// <summary>
        /// Gets all attributes whose type is convertible to the specified reflection <paramref name="type"/> using the specified <paramref name="conversionKind"/>.
        /// </summary>
        /// <param name="type">The reflection attribute type to filter by.</param>
        /// <param name="conversionKind">The kind of conversion to use when comparing types.</param>
        /// <returns>An enumeration of attributes whose type is convertible to <paramref name="type"/> according to <paramref name="conversionKind"/>.</returns>
        /// <seealso cref="ConversionKind"/>
        IEnumerable<IAttribute> OfAttributeType( Type type, ConversionKind conversionKind );

        /// <summary>
        /// Gets all attributes whose type satisfies the specified <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">A function that evaluates whether an attribute type should be included in the results.</param>
        /// <returns>An enumeration of attributes whose type matches the <paramref name="predicate"/>.</returns>
        IEnumerable<IAttribute> OfAttributeType( Func<IType, bool> predicate );

        /// <summary>
        /// Gets the constructed attributes of a given type.
        /// </summary>
        /// <typeparam name="T">The attribute type to filter by and construct.</typeparam>
        /// <returns>An enumeration of constructed attribute instances of type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method filters attributes by type <typeparamref name="T"/> and constructs them into runtime attribute instances.
        /// </remarks>
        IEnumerable<T> GetConstructedAttributesOfType<T>()
            where T : Attribute;

        /// <summary>
        /// Determines whether the collection contains any attribute whose type is convertible to the specified <paramref name="type"/> using the default conversion kind.
        /// </summary>
        /// <param name="type">The attribute type to check for.</param>
        /// <returns><c>true</c> if the collection contains at least one attribute of the specified <paramref name="type"/>; otherwise, <c>false</c>.</returns>
        /// <seealso cref="Any(IType, ConversionKind)"/>
        bool Any( IType type );

        /// <summary>
        /// Determines whether the collection contains any attribute whose type is convertible to the specified <paramref name="type"/> using the specified <paramref name="conversionKind"/>.
        /// </summary>
        /// <param name="type">The attribute type to check for.</param>
        /// <param name="conversionKind">The kind of conversion to use when comparing types.</param>
        /// <returns><c>true</c> if the collection contains at least one attribute of the specified <paramref name="type"/> according to <paramref name="conversionKind"/>; otherwise, <c>false</c>.</returns>
        /// <seealso cref="ConversionKind"/>
        bool Any( IType type, ConversionKind conversionKind );

        /// <summary>
        /// Determines whether the collection contains any attribute whose type is convertible to the specified reflection <paramref name="type"/> using the default conversion kind.
        /// </summary>
        /// <param name="type">The reflection attribute type to check for.</param>
        /// <returns><c>true</c> if the collection contains at least one attribute of the specified <paramref name="type"/>; otherwise, <c>false</c>.</returns>
        /// <seealso cref="Any(Type, ConversionKind)"/>
        bool Any( Type type );

        /// <summary>
        /// Determines whether the collection contains any attribute whose type is convertible to the specified reflection <paramref name="type"/> using the specified <paramref name="conversionKind"/>.
        /// </summary>
        /// <param name="type">The reflection attribute type to check for.</param>
        /// <param name="conversionKind">The kind of conversion to use when comparing types.</param>
        /// <returns><c>true</c> if the collection contains at least one attribute of the specified <paramref name="type"/> according to <paramref name="conversionKind"/>; otherwise, <c>false</c>.</returns>
        /// <seealso cref="ConversionKind"/>
        bool Any( Type type, ConversionKind conversionKind );
    }
}