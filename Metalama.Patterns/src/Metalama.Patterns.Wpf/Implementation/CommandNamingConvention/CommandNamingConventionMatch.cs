// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.Collections.Immutable;

namespace Metalama.Patterns.Wpf.Implementation.CommandNamingConvention;

[CompileTime]
internal sealed record CommandNamingConventionMatch(
    INamingConvention NamingConvention,
    string? CommandPropertyName,
    MemberMatch<IMemberOrNamedType, DefaultMatchKind> CommandPropertyMatch,
    MemberMatch<IMember, DefaultMatchKind> CanExecuteMatch,
    IReadOnlyList<InspectedMember> InspectedMembers,
    bool RequireCanExecuteMatch = false ) : NamingConventionMatch( NamingConvention, InspectedMembers )
{
    private static readonly ImmutableArray<string> _commandPropertyCategories = ImmutableArray.Create( CommandAttribute.CommandPropertyCategory );

    private static readonly ImmutableArray<string> _canExecuteCategories =
        ImmutableArray.Create( CommandAttribute.CanExecuteMethodCategory, CommandAttribute.CanExecutePropertyCategory );

    public override NamingConventionOutcome Outcome
    {
        get
        {
            if ( string.IsNullOrWhiteSpace( this.CommandPropertyName ) )
            {
                return NamingConventionOutcome.Mismatch;
            }
            else if ( this.CommandPropertyMatch.Outcome != MemberMatchOutcome.Success
                      || (this.CanExecuteMatch.Outcome != MemberMatchOutcome.Success
                          && (this.RequireCanExecuteMatch || this.CanExecuteMatch.Outcome != MemberMatchOutcome.NotFound)) )
            {
                return NamingConventionOutcome.Error;
            }
            else
            {
                return NamingConventionOutcome.Success;
            }
        }
    }

    protected override ImmutableArray<MemberMatchDiagnosticInfo> GetMemberDiagnostics()
        => ImmutableArray.Create(
            new MemberMatchDiagnosticInfo( this.CommandPropertyMatch, true, _commandPropertyCategories ),
            new MemberMatchDiagnosticInfo( this.CanExecuteMatch, this.RequireCanExecuteMatch, _canExecuteCategories ) );
}