// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Extensions.Architecture.Predicates;
using Metalama.Extensions.Architecture.Validators;
using Metalama.Extensions.Validation;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Text.RegularExpressions;

namespace Metalama.Extensions.Architecture.Aspects;

/// <summary>
/// Aspect that, when applied to a type, reports a warning for any derived type that does not respect a naming convention
/// given as regular expression.
/// </summary>
[PublicAPI]
public class DerivedTypesMustRespectRegexNamingConventionAttribute : TypeAspect
{
    public DerivedTypesMustRespectRegexNamingConventionAttribute( string regexPattern ) : this( regexPattern, regexPattern ) { }

    protected DerivedTypesMustRespectRegexNamingConventionAttribute( string regexPattern, string displayPattern )
    {
        this.RegexPattern = regexPattern;
        this.DisplayPattern = displayPattern;
    }

    public string DisplayPattern { get; }

    public string RegexPattern { get; }

    /// <summary>
    /// Gets a <see cref="Type"/>, derived from <see cref="ReferencePredicate"/>, that determines exclusions for the current
    /// architecture rule. Specifically, no warning will be reported if the  <see cref="ReferencePredicate.IsMatch"/> method
    /// of the <see cref="ReferencePredicate"/> returns <c>true</c>. This type must have a default constructor.
    /// </summary>
    public Type? ExclusionPredicateType { get; init; }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Validate the predicate type.
        var predicateBuilder = new ReferencePredicateBuilder( ReferenceEndRole.Origin, builder );

        if ( !ExclusionPredicateTypeHelper.TryCreateExclusionPredicate(
                this.ExclusionPredicateType,
                builder.Diagnostics,
                predicateBuilder,
                out var exclusionPredicate ) )
        {
            builder.SkipAspect();

            return;
        }

        // Validate the regex.
        try
        {
            _ = new Regex( this.RegexPattern );
        }
        catch ( Exception e )
        {
            builder.Diagnostics.Report( ArchitectureDiagnosticDefinitions.InvalidRegex.WithArguments( (this.RegexPattern, e.Message) ) );
            builder.SkipAspect();

            return;
        }

        builder.Outbound.ValidateInboundReferences(
            DerivedTypeNamingConventionValidator.CreateRegexValidator(
                this.RegexPattern,
                this.DisplayPattern,
                exclusionPredicate ) );
    }

    public override string ToString() => $"DerivedTypesMustRespectRegexNamingConvention( \"{this.DisplayPattern}\" )";
}