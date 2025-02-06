// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal abstract class DiagnosticReporter
{
    private readonly IAspectBuilder _builder;

    protected DiagnosticReporter( IAspectBuilder builder )
    {
        this._builder = builder;
    }

    protected abstract string GeneratedArtifactKind { get; }

    // ReSharper disable once UnusedParameter.Global

    /// <summary>
    /// Gets a the <c>TargetDeclarationDescription</c> argument for <see cref="Diagnostics.ValidCandidateDeclarationIsAmbiguous"/> and <see cref="Diagnostics.InvalidCandidateDeclarationSignature"/>.
    /// </summary>
    protected abstract string GetTargetDeclarationDescription();

    /// <summary>
    /// Gets the <c>InvalidityReason</c> argument for <see cref="Diagnostics.InvalidCandidateDeclarationSignature"/>.
    /// </summary>
    /// <param name="inspectedMember"></param>
    /// <returns></returns>
    protected abstract string GetInvalidityReason( in InspectedMember inspectedMember );

    private void ReportAmbiguousDeclaration( INamingConvention namingConvention, in InspectedMember inspectedMember, bool isRequired )
        => this._builder.Diagnostics.Report(
            Diagnostics.ValidCandidateDeclarationIsAmbiguous.WithArguments(
                (
                    inspectedMember.Member.DeclarationKind,
                    inspectedMember.Category,
                    this.GetTargetDeclarationDescription(),
                    this._builder.Target,
                    isRequired ? "as required " : null,
                    namingConvention.Name
                ) ),
            inspectedMember.Member );

    private void ReportConflictingDeclaration(
        INamingConvention namingConvention,
        IDeclaration conflictingDeclaration,
        IEnumerable<string> applicableCategories,
        bool isRequired )
        => this._builder.Diagnostics.Report(
            Diagnostics.ExistingMemberNameConflict.WithArguments(
                (
                    conflictingDeclaration.DeclarationKind,
                    conflictingDeclaration,
                    (this._builder.Target as IMemberOrNamedType)?.DeclaringType!,
                    isRequired ? "required " : null,
                    applicableCategories.PrettyList( " or " ),
                    namingConvention.Name) ) );

    private void ReportInvalidDeclaration( INamingConvention namingConvention, in InspectedMember inspectedMember, bool isRequired )
        => this._builder.Diagnostics.Report(
            Diagnostics.InvalidCandidateDeclarationSignature.WithArguments(
                (
                    inspectedMember.Member,
                    inspectedMember.Member.DeclarationKind,
                    inspectedMember.Category,
                    this.GetTargetDeclarationDescription(),
                    this._builder.Target,
                    isRequired ? "as required " : null,
                    namingConvention.Name,
                    this.GetInvalidityReason( inspectedMember )
                ) ),
            inspectedMember.Member );

    private void ReportDeclarationNotFound(
        IDeclaration principalDeclaration,
        IEnumerable<string> candidateNames,
        IEnumerable<string> applicableCategories )
    {
        var candidateNamesList = candidateNames.PrettyList( " or ", out _, '\'' );

        this._builder.Diagnostics.Report(
            Diagnostics.CandidateNamesNotFound.WithArguments(
                (
                    applicableCategories.PrettyList( " or " ),
                    this.GeneratedArtifactKind,
                    candidateNamesList,
                    principalDeclaration
                ) ) );
    }

    public void ReportNoNamingConventionMatched( IDeclaration principalDeclaration )
        => this._builder.Diagnostics.Report( Diagnostics.NoNamingConventionMatched.WithArguments( (this.GeneratedArtifactKind, principalDeclaration) ) );

    private void ReportDiagnostics(
        IDeclaration principalDeclaration,
        INamingConvention namingConvention,
        MemberMatchDiagnosticInfo matchedMember,
        IReadOnlyList<InspectedMember> inspectedDeclarations )
    {
        switch ( matchedMember.Match.Outcome )
        {
            case MemberMatchOutcome.Ambiguous:

                foreach ( var inspectedDeclaration in inspectedDeclarations )
                {
                    if ( inspectedDeclaration.IsValid && matchedMember.Categories.Any( c => c == inspectedDeclaration.Category ) )
                    {
                        this.ReportAmbiguousDeclaration( namingConvention, inspectedDeclaration, matchedMember.IsRequired );
                    }
                }

                break;

            case MemberMatchOutcome.Invalid:

                foreach ( var inspectedDeclaration in inspectedDeclarations )
                {
                    if ( !inspectedDeclaration.IsValid && matchedMember.Categories.Any( c => c == inspectedDeclaration.Category ) )
                    {
                        this.ReportInvalidDeclaration( namingConvention, inspectedDeclaration, matchedMember.IsRequired );
                    }
                }

                break;

            case MemberMatchOutcome.NotFound:

                if ( !matchedMember.Match.CandidateNames.IsEmpty && matchedMember.IsRequired )
                {
                    this.ReportDeclarationNotFound( principalDeclaration, matchedMember.Match.CandidateNames, matchedMember.Categories );
                }

                break;

            case MemberMatchOutcome.Conflict:

                this.ReportConflictingDeclaration( namingConvention, matchedMember.Match.Member!, matchedMember.Categories, matchedMember.IsRequired );

                break;
        }
    }

    public void ReportNamingConventionFailure<TDeclaration, TMatch>(
        INamingConvention<TDeclaration, TMatch> namingConvention,
        TDeclaration target,
        TMatch match )
        where TMatch : NamingConventionMatch
        where TDeclaration : IDeclaration
    {
        foreach ( var memberDiagnostic in match.Members )
        {
            this.ReportDiagnostics( target, namingConvention, memberDiagnostic, match.InspectedMembers );
        }
    }
}