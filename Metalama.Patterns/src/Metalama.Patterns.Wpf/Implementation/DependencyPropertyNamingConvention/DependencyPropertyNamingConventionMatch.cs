// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.Collections.Immutable;

namespace Metalama.Patterns.Wpf.Implementation.DependencyPropertyNamingConvention;

[CompileTime]
internal sealed record DependencyPropertyNamingConventionMatch(
    INamingConvention NamingConvention,
    string? DependencyPropertyName,
    string? RegistrationFieldName,
    MemberMatch<IMemberOrNamedType, DefaultMatchKind> RegistrationFieldConflictMatch,
    MemberMatch<IMethod, ChangeHandlerSignatureKind> PropertyChangedMatch,
    MemberMatch<IMethod, ValidationHandlerSignatureKind> ValidateMatch,
    IReadOnlyList<InspectedMember> InspectedMembers,
    bool RequirePropertyChangedMatch = false,
    bool RequireValidateMatch = false ) : NamingConventionMatch( NamingConvention, InspectedMembers )
{
    public override NamingConventionOutcome Outcome
    {
        get
        {
            if ( string.IsNullOrWhiteSpace( this.DependencyPropertyName )
                 || string.IsNullOrWhiteSpace( this.RegistrationFieldName ) )
            {
                return NamingConventionOutcome.Mismatch;
            }

            if ( this.RegistrationFieldConflictMatch.Outcome != MemberMatchOutcome.Success
                 || (this.PropertyChangedMatch.Outcome != MemberMatchOutcome.Success
                     && (this.RequirePropertyChangedMatch || this.PropertyChangedMatch.Outcome != MemberMatchOutcome.NotFound))
                 || (this.ValidateMatch.Outcome != MemberMatchOutcome.Success
                     && (this.RequireValidateMatch || this.ValidateMatch.Outcome != MemberMatchOutcome.NotFound)) )
            {
                return NamingConventionOutcome.Error;
            }

            return NamingConventionOutcome.Success;
        }
    }

    protected override ImmutableArray<MemberMatchDiagnosticInfo> GetMemberDiagnostics()
        => ImmutableArray.Create(
            new MemberMatchDiagnosticInfo( this.RegistrationFieldConflictMatch, true, _registrationFieldCategories ),
            new MemberMatchDiagnosticInfo( this.PropertyChangedMatch, this.RequirePropertyChangedMatch, _propertyChangedCategories ),
            new MemberMatchDiagnosticInfo( this.ValidateMatch, this.RequireValidateMatch, _validateCategories ) );

    private static readonly ImmutableArray<string> _registrationFieldCategories =
        ImmutableArray.Create( DependencyPropertyAspectBuilder.RegistrationFieldCategory );

    private static readonly ImmutableArray<string> _propertyChangedCategories =
        ImmutableArray.Create( DependencyPropertyAspectBuilder.PropertyChangedMethodCategory );

    private static readonly ImmutableArray<string> _validateCategories = ImmutableArray.Create( DependencyPropertyAspectBuilder.ValidateMethodCategory );
}