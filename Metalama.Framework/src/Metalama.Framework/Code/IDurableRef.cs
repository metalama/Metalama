// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// An <see cref="IRef"/> that stores only a string identifier and therefore does not keep the compilation it came
    /// from in memory. Its <see cref="IRef.IsDurable"/> property is always <c>true</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this type in the signature of anything that stores a reference for longer than a single pipeline run: a field
    /// of a fabric, a field of an inheritable aspect, or a parameter of an API that keeps what it is given. The compiler
    /// then enforces what would otherwise only be a comment, because an ordinary <see cref="IRef{T}"/> obtained from
    /// <see cref="IDeclaration.ToRef"/> holds the compilation and cannot be converted implicitly.
    /// </para>
    /// <para>
    /// Call <see cref="IRef.ToDurable"/> to obtain one. Resolving a durable reference costs an identifier lookup, which
    /// is why references are not durable by default.
    /// </para>
    /// </remarks>
    /// <seealso cref="IDurableRef{T}"/>
    /// <seealso cref="IRef.IsDurable"/>
    /// <seealso cref="IRef.ToDurable"/>
    [CompileTime]
    [InternalImplement]
    public interface IDurableRef : IRef { }
}
