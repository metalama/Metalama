// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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