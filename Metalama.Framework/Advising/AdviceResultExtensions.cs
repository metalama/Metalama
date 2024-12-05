// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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