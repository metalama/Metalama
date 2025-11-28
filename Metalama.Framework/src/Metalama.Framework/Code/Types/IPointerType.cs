// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.Types
{
    /// <summary>
    /// Represents an unsafe pointer type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To create an instance of <see cref="IPointerType"/>, use <see cref="IType.MakePointerType"/> on an existing type.
    /// For example, to create <c>int*</c>, use <c>TypeFactory.GetType(SpecialType.Int32).MakePointerType()</c>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IType"/>
    /// <seealso cref="IType.MakePointerType"/>
    /// <seealso cref="IArrayType"/>
    /// <seealso cref="IFunctionPointerType"/>
    /// <seealso href="@type-system"/>
    public interface IPointerType : IType
    {
        /// <summary>
        /// Gets the type pointed at, that is, <c>T</c> for <c>T*</c>.
        /// </summary>
        IType PointedAtType { get; }
    }
}