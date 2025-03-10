// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Comparers
{
    /// <summary>
    /// An umbrella interface for an equality comparer of <see cref="IDeclaration"/> and <see cref="IType"/>.
    /// </summary>
    [CompileTime]
    public interface IDeclarationComparer : IEqualityComparer<IDeclaration>, ITypeComparer;
}