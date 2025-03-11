// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal static class NamingConventionMatcher
{
    public static MemberMatch<TMember, TKind> FindMatchingMembers<TMember, TKind>(
        this IEnumerable<TMember> members,
        INameMatchPredicate predicate,
        Func<TMember, TKind?> classifyMember,
        Action<InspectedMember> addInspectedMember,
        string category )
        where TMember : class, IMemberOrNamedType
        where TKind : struct
    {
        // NB: The loop is not short-circuited because validity checking will build a list of
        // inspected declarations which will be used for diagnostics when applicable.

        var candidateCount = 0;
        var eligibleCount = 0;
        TMember? firstEligible = null;
        TKind? firstEligibleKind = null;

        foreach ( var declaration in members )
        {
            if ( predicate.IsMatch( declaration.Name ) )
            {
                ++candidateCount;

                var kind = classifyMember( declaration );

                if ( kind != null )
                {
                    ++eligibleCount;
                    firstEligible ??= declaration;
                    firstEligibleKind ??= kind;
                }

                addInspectedMember( new InspectedMember( declaration, kind != null, category ) );
            }
        }

        switch ( eligibleCount )
        {
            case 0:
                if ( candidateCount > 0 )
                {
                    return MemberMatch<TMember, TKind>.Invalid();
                }

                break;

            case 1:
                return MemberMatch<TMember, TKind>.Success( firstEligible!, firstEligibleKind!.Value );

            case > 1:
                return MemberMatch<TMember, TKind>.Ambiguous();
        }

        return MemberMatch<TMember, TKind>.NotFound( predicate.Candidates );
    }
}