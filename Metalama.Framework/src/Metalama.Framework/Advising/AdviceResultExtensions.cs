// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Advising;

/// <summary>
/// Extension methods for the <see cref="IIntroductionAdviceResult{T}"/> interface.
/// </summary>
[PublicAPI]
public static class AdviceResultExtensions
{
    /// <summary>
    /// Attempts to get the declaration (method, property, field, event, type, or operator) that was introduced by the advice, if the advice was successful.
    /// </summary>
    /// <typeparam name="T">The type of declaration (e.g., <see cref="IMethod"/>, <see cref="IProperty"/>, <see cref="IField"/>, <see cref="IEvent"/>, <see cref="INamedType"/>).</typeparam>
    /// <param name="adviceResult">The advice result to check.</param>
    /// <param name="declaration">When this method returns <c>true</c>, contains the introduced or overridden declaration; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the advice successfully introduced or overrode a declaration (outcomes: <see cref="AdviceOutcome.Default"/>, <see cref="AdviceOutcome.Override"/>, or <see cref="AdviceOutcome.New"/>); otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// When the introduced declaration is a type, the <see cref="IIntroductionAdviceResult{T}"/> interface itself implements <see cref="IAdviser{T}"/> and can be used to introduce members to the type.
    /// </remarks>
    public static bool TryGetDeclaration<T>( this IIntroductionAdviceResult<T> adviceResult, [NotNullWhen( true )] out T? declaration )
        where T : class, IDeclaration
    {
        if ( adviceResult.Outcome is AdviceOutcome.Default or AdviceOutcome.Override or AdviceOutcome.New )
        {
            declaration = adviceResult.Declaration;

            return true;
        }
        else
        {
            declaration = null;

            return false;
        }
    }
}