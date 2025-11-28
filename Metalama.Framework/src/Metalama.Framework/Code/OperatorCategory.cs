// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code;

/// <summary>
/// Categorizes operator kinds. To get the <see cref="OperatorCategory"/> from an <see cref="OperatorKind"/>,
/// use the <see cref="OperatorKindExtensions.GetCategory"/> extension method.
/// </summary>
/// <seealso cref="OperatorKind"/>
/// <seealso cref="OperatorKindExtensions.GetCategory"/>
/// <seealso cref="IMethod"/>
[CompileTime]
public enum OperatorCategory
{
    None,
    Unary,
    Binary,
    Conversion,
    BinaryAssignment,
    UnaryAssignment
}