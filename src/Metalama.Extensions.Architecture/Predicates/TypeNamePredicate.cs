// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;

namespace Metalama.Extensions.Architecture.Predicates;

internal class TypeNamePredicate : BaseNamePredicate
{
    public TypeNamePredicate( string name, ReferencePredicateBuilder builder ) : base( name, builder ) { }

    public override bool IsMatch( ReferenceEnd referenceEnd ) => this.IsMatch( referenceEnd.Type.FullName );

    protected override ReferenceGranularity GetGranularity() => ReferenceGranularity.Type;
}