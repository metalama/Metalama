// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.Comparers;

/// <summary>
/// Specifies which comparer should be used.
/// </summary>
/// <seealso cref="ICompilation.Comparers"/>
[CompileTime]
public enum TypeComparison
{
    /// <summary>
    /// Does not take nullability into account.
    /// </summary>
    Default,

    /// <summary>
    /// Takes nullability into account.
    /// </summary>
    IncludeNullability
}