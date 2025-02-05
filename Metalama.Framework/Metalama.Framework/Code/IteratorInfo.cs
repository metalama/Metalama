// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code;

/// <summary>
/// Information about an iterator method, returned by the <see cref="MethodExtensions.GetIteratorInfo"/> extension method of <see cref="IMethod"/>.
/// </summary>    
[CompileTime]
public readonly struct IteratorInfo
{
    private readonly IType? _returnType;

    /// <summary>
    /// Gets a value indicating whether the method is an iterator (i.e., has a <c>yield return</c> or <c>yield break</c> statement).
    /// This property evaluates to <c>false</c> for methods that return an enumerable type but do not use <c>yield</c>,
    /// and <c>null</c> for methods that are not defined in the current project.
    /// </summary>
    public bool? IsIteratorMethod { get; }

    /// <summary>
    /// Gets the type of items being enumerated (the <c>int</c> in <c>IEnumerable&lt;int&gt;</c>).
    /// </summary>
    public IType ItemType
    {
        get
        {
            if ( this._returnType == null )
            {
                throw new InvalidOperationException( $"Cannot get the {nameof(this.ItemType)} property because the return type is not available." );
            }

            if ( this._returnType is INamedType { TypeArguments.Count: > 0 } namedType )
            {
                return namedType.TypeArguments[0];
            }
            else
            {
                return TypeFactory.GetType( SpecialType.Object );
            }
        }
    }

    /// <summary>
    /// Gets the kind of enumerable (<see cref="Code.EnumerableKind.IEnumerable"/>, <see cref="Code.EnumerableKind.IEnumerator"/>,
    /// <see cref="Code.EnumerableKind.IAsyncEnumerable"/>, ...), regardless of whether the method is a yield-base iterator (see <see cref="IsIteratorMethod"/>).
    /// </summary>
    public EnumerableKind EnumerableKind { get; }

    internal IteratorInfo( bool? isIteratorMethod, EnumerableKind enumerableKind, IType? returnType )
    {
        this._returnType = returnType;
        this.EnumerableKind = enumerableKind;
        this.IsIteratorMethod = isIteratorMethod;
    }
}