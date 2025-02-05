// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace Metalama.Framework.Serialization
{
    /// <summary>
    /// Marks the implementing type as being serializable. Aspects and fabrics are typically serialized when they affect a different project than the current one (by inheritance
    /// or reference validation). Serialized objects may be deserialized on a different machine than the one on which they have been serialized, and
    /// a long time after.
    /// </summary>
    /// <remarks>
    /// <para>When a type is marked as compile-time-serializable, all fields and automatic properties should be of a serializable type, except those
    /// annotated with the <see cref="NonCompileTimeSerializedAttribute"/> custom attribute. </para>
    /// <para>The following system types are serializable: intrinsic types, arrays of serializable types, <see cref="DateTime"/>, <see cref="TimeSpan"/>, <see cref="CultureInfo"/>, <see cref="Guid"/>, <see cref="Dictionary{TKey,TValue}"/>,
    /// <see cref="List{T}"/>, <see cref="ImmutableArray{T}"/>, <see cref="ImmutableDictionary{TKey,TValue}"/>.
    /// </para>
    /// <para>To serialize another system type, implement a <see cref="ReferenceTypeSerializer"/> or <see cref="ValueTypeSerializer{T}"/> and register it by
    /// adding a <see cref="ImportSerializerAttribute"/> custom attribute to the types that uses it.</para>
    /// <para>To serialize an <see cref="IDeclaration"/>, use the <see cref="IDeclaration.ToRef"/> method and store the <see cref="IRef{T}"/>.</para>
    /// </remarks>
    [CompileTime]
    public interface ICompileTimeSerializable;
}