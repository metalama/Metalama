// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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