// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using static Metalama.Framework.Diagnostics.Severity;

namespace Metalama.Patterns.Wpf.Implementation;

// ReSharper disable InconsistentNaming
[CompileTime]
internal static class Diagnostics
{
    private const string _category = "Metalama.Patterns.Wpf";

    // Reserved range 5200-5220

    /// <summary>
    /// The CanExecuteMethod and CanExecuteProperty properties cannot both be defined at the same time.
    /// </summary>
    public static readonly DiagnosticDefinition CannotSpecifyBothCanExecuteMethodAndCanExecuteProperty =
        new(
            "LAMA5201",
            Error,
            $"The {nameof(CommandAttribute.CanExecuteMethod)} and {nameof(CommandAttribute.CanExecuteProperty)} properties cannot both be defined at the same time.",
            $"Invalid {nameof(CommandAttribute)} properties." );

    /// <summary>
    /// The can-execute property for command method {0} is not public, and INotifyPropertyChanged integration is enabled and applicable.
    /// Because the can-execute property is not public, INotifyPropertyChanged.PropertyChanged events might not be raised depending on the INotifyPropertyChanged implementation.
    /// </summary>
    public static readonly DiagnosticDefinition<IMethod> CommandNotifiableCanExecutePropertyIsNotPublic =
        new(
            "LAMA5202",
            Warning,
            "The can-execute property for command method {0} is not public, and INotifyPropertyChanged integration is enabled and applicable. " +
            "Because the can-execute property is not public, INotifyPropertyChanged.PropertyChanged events might not be raised depending on the INotifyPropertyChanged implementation.",
            "Notifiable can-execute property is not public." );

    /// <summary>
    /// The name of existing {0} {1}, that is defined in or inherited by class {2}, conflicts with the {3}{4} name determined by the {5} naming convention.
    /// </summary>
    public static readonly DiagnosticDefinition<(DeclarationKind ConflictingDeclarationKind, IDeclaration ConflictingDeclaration, INamedType DeclaringType,
            string? Required, string IntroducedMemberDescription, string NamingConvention)>
        ExistingMemberNameConflict =
            new(
                "LAMA5203",
                Error,
                "The name of existing {0} {1}, defined in or inherited by class {2}, conflicts with the {3}{4} name determined by the {5} naming convention.",
                "Existing member conflicts with member to be introduced.",
                _category );

    /// <summary>
    /// To be applied to each invalid member:
    /// The {0} was identified as a candidate {1} for {2}{3} {4}by the {5} naming convention, but the signature is not valid.{6}
    /// For example, "The `method` was a candidate `can-execute method` for `[Command] method ``Foo()`, but the signature is not valid.` The method must blah blah.`".
    /// </summary>
    public static readonly DiagnosticDefinition<(
            IDeclaration DiagnosticTargetDeclaration,
            DeclarationKind DiagnosticTargetDeclarationKind,
            string? CandidateDescription,
            string TargetDeclarationDescription,
            IDeclaration TargetDeclaration,
            string? AsRequired,
            string NamingConvention,
            string? InvalidityReason
            )>
        InvalidCandidateDeclarationSignature =
            new(
                "LAMA5204",
                Error,
                "The '{0}' {1} has an invalid signature and cannot be used as a {2} for {3}'{4}' {5}by the '{6}' naming convention.{7}",
                "Invalid candidate member signature.",
                _category );

    /// <summary>
    /// The {0} was identified as a valid candidate {1} for {2}{3} {4}by the {5} naming convention, but other members also matched.
    /// </summary>
    public static readonly DiagnosticDefinition<(DeclarationKind DiagnosticTargetDeclaration, string? CandidateDescription, string TargetDeclarationDescription,
            IDeclaration TargetDeclaration, string? AsRequired, string NamingConvention)>
        ValidCandidateDeclarationIsAmbiguous =
            new(
                "LAMA5205",
                Error,
                "Ambiguous match while identifying the {1} for {2}'{3}' {4}by the '{5}' naming convention.",
                "Ambiguous candidate member.",
                _category );

    /// <summary>
    /// No {0} was found {1} the {2} naming convention, with candidate member name{3} {4}.
    /// </summary>
    public static readonly DiagnosticDefinition<(
            string CandidateDescription,
            string MemberKind,
            string CandidateNames,
            IDeclaration Declaration
            )>
        CandidateNamesNotFound =
            new(
                "LAMA5206",
                Error,
                "No {0} matching the {1} {2} was found for '{3}'.",
                "Optional member not found.",
                _category );

    /// <summary>
    /// No match was found using the {0} naming convention{1}. See other warnings for details.
    /// </summary>
    public static readonly DiagnosticDefinition<(string GeneratedArtifactKind, IDeclaration Declaration)>
        NoNamingConventionMatched =
            new(
                "LAMA5207",
                Error,
                "No {0} naming conventioned matched '{1}'.",
                "No configured naming convention matched.",
                _category );

    public static readonly DiagnosticDefinition<IMethod>
        CancellationInNonAsyncNotSupported =
            new(
                "LAMA5208",
                Error,
                "The '{0}' method has a CancellationToken parameter, but is neither returns a Task nor has the Background property set to true.",
                "The method has a CancellationToken parameter, but is neither returns a Task nor has the Background property set to true.",
                _category );
}