// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Architecture.Predicates;
using Metalama.Extensions.Validation;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Extensions.Architecture.Validators;

[CompileTime]
public class ReferencePredicateValidator : InboundReferenceValidator
{
    private readonly ReferencePredicate _allowedScope;
    private readonly string? _description;

    public override ReferenceKinds ValidatedReferenceKinds { get; }

    public ReferencePredicateValidator( ReferencePredicate allowedScope, string? description, ReferenceKinds validatedReferenceKinds )
    {
        this._allowedScope = allowedScope;
        this._description = description;
        this.ValidatedReferenceKinds = validatedReferenceKinds;
    }

    /// <summary>
    /// Validates a reference and reports a warning if necessary.
    /// </summary>
    public override void ValidateReferences( ReferenceValidationContext context )
    {
        if ( !this._allowedScope.IsMatch( context ) )
        {
            var referencedDeclaration = context.Destination.Declaration;
            var referencedNamedType = referencedDeclaration.GetClosestNamedType();
            var referencingDeclaration = context.Origin.Declaration;

            var optionalSpace = string.IsNullOrEmpty( this._description ) ? "" : " ";

            // Report the error message.
            context.Diagnostics.Report(
                r =>
                {
                    // Do not validate inside the same type.
                    // We are testing this here and not in a predicate because it allows to keep the granularity at namespace level
                    // without affecting performance too much since this code would only run in case positive match.
                    if ( referencedNamedType != null && r.OriginDeclaration.IsContainedIn( referencedNamedType ) )
                    {
                        return null;
                    }

                    // Return the error message.
                    var usageKind = r.ReferenceKind == ReferenceKinds.Assignment ? "assigned" : "referenced";

                    return ArchitectureDiagnosticDefinitions.OnlyAccessibleFrom.WithArguments(
                        (referencedDeclaration,
                         referencedDeclaration.DeclarationKind,
                         usageKind,
                         referencingDeclaration,
                         referencingDeclaration.DeclarationKind,
                         optionalSpace,
                         this._description) );
                } );
        }
    }

    public override ReferenceGranularity Granularity => this._allowedScope.Granularity;
}