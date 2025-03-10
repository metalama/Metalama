// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Advising;

[PublicAPI]
public static class AdviceResultExtensions
{
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