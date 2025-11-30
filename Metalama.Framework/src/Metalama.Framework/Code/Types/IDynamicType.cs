// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.Types
{
    /// <summary>
    /// Represents the <c>dynamic</c> type, which bypasses compile-time type checking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>dynamic</c> type in C# enables dynamic binding at run time. Operations involving
    /// dynamic types are resolved at run time rather than compile time.
    /// </para>
    /// <para>
    /// In the Metalama type system, <see cref="IDynamicType"/> is a distinct type kind from <see cref="INamedType"/>.
    /// You can check for the dynamic type using <see cref="IType.TypeKind"/> returning <see cref="TypeKind.Dynamic"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IType"/>
    /// <seealso cref="TypeKind.Dynamic"/>
    /// <seealso href="@type-system"/>
    public interface IDynamicType : IType
    {
        new IDynamicType ToNullable();

        new IDynamicType ToNonNullable();
    }
}