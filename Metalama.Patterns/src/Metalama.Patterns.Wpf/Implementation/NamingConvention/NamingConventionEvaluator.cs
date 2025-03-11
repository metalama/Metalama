// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal static class NamingConventionEvaluator
{
    public static bool TryEvaluate<TDeclaration, TMatch>(
        IReadOnlyCollection<INamingConvention<TDeclaration, TMatch>> namingConventions,
        TDeclaration target,
        DiagnosticReporter diagnosticReporter,
        [NotNullWhen( true )] out TMatch? match )
        where TMatch : NamingConventionMatch
        where TDeclaration : IDeclaration
    {
        foreach ( var namingConvention in namingConventions )
        {
            var result = namingConvention.Match( target );

            switch ( result.Outcome )
            {
                case NamingConventionOutcome.Success:
                    match = result;

                    return true;

                case NamingConventionOutcome.Warning:
                    diagnosticReporter.ReportNamingConventionFailure( namingConvention, target, result );
                    match = result;

                    return true;

                case NamingConventionOutcome.Error:
                    diagnosticReporter.ReportNamingConventionFailure( namingConvention, target, result );
                    match = result;

                    return false;

                default:
                    continue;
            }
        }

        // No naming convention matched.
        diagnosticReporter.ReportNoNamingConventionMatched( target );

        match = null;

        return false;
    }
}