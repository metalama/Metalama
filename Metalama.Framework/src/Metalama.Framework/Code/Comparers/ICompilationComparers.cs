// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Comparers;

/// <summary>
/// Exposes comparers of different characteristics. To get an instance of this interface, use the <see cref="ICompilation.Comparers"/> property.
/// </summary>
/// <seealso cref="IDeclarationComparer"/>
/// <seealso cref="ITypeComparer"/>
/// <seealso cref="TypeComparison"/>
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

    /// <summary>
    /// Gets a deterministic ordering comparer for <see cref="IDeclaration"/>. The specific order produced by the
    /// comparer is an implementation detail — it is guaranteed to be stable across builds of the same compilation
    /// but the exact ordering is not part of the public contract. Sort key: depth, then containing declaration
    /// (recursively), then name, then signature (for overloadable members).
    /// </summary>
    IComparer<IDeclaration> DeterministicDeclarationOrder { get; }

    /// <summary>
    /// Gets a deterministic ordering comparer for <see cref="IType"/>. Sort key: kind, then — for named types —
    /// containing namespace, declaring type, name, and type arguments; arrays by rank and element type; pointers
    /// by pointed-at type; type parameters by kind and index. The specific order is an implementation detail
    /// but is stable.
    /// </summary>
    IComparer<IType> DeterministicTypeOrder { get; }
}