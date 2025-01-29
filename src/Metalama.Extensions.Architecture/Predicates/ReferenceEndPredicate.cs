// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Extensions.Validation;

namespace Metalama.Extensions.Architecture.Predicates;

/// <summary>
/// A <see cref="ReferencePredicate"/> that relies on a single <see cref="ReferenceEnd"/> (referencing or referenced).
/// </summary>
[PublicAPI]
public abstract class ReferenceEndPredicate : ReferencePredicate
{
    /// <summary>
    /// Gets the role of the <see cref="ReferenceEnd"/> being validated.
    /// </summary>
    public ReferenceEndRole ValidatedRole { get; }

    protected ReferenceEndPredicate( ReferencePredicateBuilder builder ) : base( builder )
    {
        this.ValidatedRole = builder.Context.ValidatedRole;
    }

    protected sealed override bool IsMatchCore( ReferenceValidationContext context ) => this.IsMatch( context.GetReferenceEnd( this.ValidatedRole ) );

    /// <summary>
    /// Gets a value indicating whether the predicate matches the given <see cref="ReferenceEnd"/>.
    /// </summary>
    public abstract bool IsMatch( ReferenceEnd referenceEnd );
}