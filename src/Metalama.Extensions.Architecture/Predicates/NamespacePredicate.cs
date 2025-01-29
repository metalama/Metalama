// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;

namespace Metalama.Extensions.Architecture.Predicates;

internal class NamespacePredicate : BaseNamePredicate
{
    public NamespacePredicate( string name, ReferencePredicateBuilder builder ) : base( name, builder ) { }

    public override bool IsMatch( ReferenceEnd referenceEnd )
    {
        for ( var ns = referenceEnd.Namespace; ns != null; ns = ns.ContainingNamespace )
        {
            if ( this.IsMatch( ns.FullName ) )
            {
                return true;
            }
        }

        return false;
    }

    protected override ReferenceGranularity GetGranularity() => ReferenceGranularity.Namespace;
}