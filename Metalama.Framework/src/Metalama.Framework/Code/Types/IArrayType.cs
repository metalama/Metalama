// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.Types
{
    /// <summary>
    /// Represents an array, e.g. <c>T[]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To create an <see cref="IArrayType"/>, call the <see cref="IType.MakeArrayType"/> method on any <see cref="IType"/> instance.
    /// For example: <c>intType.MakeArrayType()</c> creates <c>int[]</c>, and <c>intType.MakeArrayType(2)</c> creates <c>int[,]</c>.
    /// </para>
    /// <para>
    /// You can also obtain array types from existing declarations in the code model.
    /// </para>
    /// </remarks>
    /// <seealso cref="IType"/>
    /// <seealso cref="IType.MakeArrayType"/>
    /// <seealso cref="IPointerType"/>
    /// <seealso href="@type-system"/>
    public interface IArrayType : IType
    {
        /// <summary>
        /// Gets the element type, e.g. the <c>T</c> in <c>T[]</c>.
        /// </summary>
        IType ElementType { get; }

        /// <summary>
        /// Gets the array rank (1 for <c>T[]</c>, 2 for <c>T[,]</c>, ...).
        /// </summary>
        int Rank { get; }

        /// <inheritdoc cref="IType.ToNullable"/>
        new IArrayType ToNullable();

        /// <inheritdoc cref="IType.ToNonNullable"/>
        new IArrayType ToNonNullable();

        /// <inheritdoc cref="IType.StripNullabilityAnnotation"/>
        new IArrayType StripNullabilityAnnotation();
    }
}