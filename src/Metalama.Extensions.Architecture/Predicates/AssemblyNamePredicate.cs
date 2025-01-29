// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;

namespace Metalama.Extensions.Architecture.Predicates;

internal class AssemblyNamePredicate : BaseNamePredicate
{
    public AssemblyNamePredicate( string name, ReferencePredicateBuilder builder ) : base( name, builder ) { }

    public override bool IsMatch( ReferenceEnd referenceEnd ) => this.IsMatch( referenceEnd.Assembly.Identity.Name );

    protected override ReferenceGranularity GetGranularity() => ReferenceGranularity.Compilation;
}