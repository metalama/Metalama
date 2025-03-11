// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Comparers;

/// <summary>
/// Exposes comparers of different characteristics.
/// </summary>
[CompileTime]
[InternalImplement]
public interface ICompilationComparers
{
    /// <summary>
    /// Gets an <see cref="IEqualityComparer{T}"/> allowing to compare types and declarations considers equal two instances that represent
    /// the same type or declaration even if they belong to different compilation versions. This comparer ignores
    /// the nullability annotations of reference types.
    /// </summary>
    IDeclarationComparer Default { get; }

    /// <summary>
    /// Gets an <see cref="IEqualityComparer{T}"/> allowing to compare types and declarations considers equal two instances that represent
    /// the same type or declaration even if they belong to different compilation versions. This comparer takes
    /// the nullability annotations of reference types into account.
    /// </summary>
    ITypeComparer IncludeNullability { get; }

    ITypeComparer GetTypeComparer( TypeComparison comparison );
}