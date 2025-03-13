// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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