// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Comparers;

/// <summary>
/// Extends <see cref="IEqualityComparer{T}"/> of <see cref="IType"/> with the method <see cref="IsConvertibleTo(Metalama.Framework.Code.IType,Metalama.Framework.Code.IType,Metalama.Framework.Code.ConversionKind)"/>,
/// which checks for type inheritance and not equality.
/// </summary>
[CompileTime]
public interface ITypeComparer : IEqualityComparer<IType>, IEqualityComparer<INamedType>
{
    [Obsolete( "This method has been renamed IsConvertibleTo." )]
    bool Is( IType left, IType right, ConversionKind kind = ConversionKind.Default );

    /// <summary>
    /// Equivalent to the <c>is</c> operator in C#. Gets a value indicating whether the left <see cref="IType"/> is assignable to right <see cref="IType"/>.
    /// </summary>
    /// <returns></returns>
    bool IsConvertibleTo( IType left, IType right, ConversionKind kind = ConversionKind.Default );

    [Obsolete( "This method has been renamed IsConvertibleTo." )]
    bool Is( IType left, Type right, ConversionKind kind = ConversionKind.Default );

    /// <summary>
    /// Equivalent to the <c>is</c> operator in C#. Gets a value indicating whether the left <see cref="Type"/> is assignable to right <see cref="Type"/>.
    /// </summary>
    /// <returns></returns>
    bool IsConvertibleTo( IType left, Type right, ConversionKind kind = ConversionKind.Default );
}