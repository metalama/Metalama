// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.Types;

/// <summary>
/// Represents a function pointer type (<c>delegate*</c>).
/// </summary>
/// <remarks>
/// Function pointer types are not fully supported in Metalama.
/// </remarks>
/// <seealso cref="IType"/>
/// <seealso cref="IPointerType"/>
/// <seealso cref="TypeKind.FunctionPointer"/>
/// <seealso href="@type-system"/>
public interface IFunctionPointerType : IType;