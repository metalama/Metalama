// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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