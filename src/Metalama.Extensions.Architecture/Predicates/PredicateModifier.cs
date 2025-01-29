// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;

namespace Metalama.Extensions.Architecture.Predicates;

[RunTimeOrCompileTime]
internal abstract class PredicateModifier : ICompileTimeSerializable
{
    public abstract bool IsMatch( bool currentPredicateResult, ReferenceValidationContext context );

    public abstract ReferenceGranularity ModifyGranularity( ReferenceGranularity baseGranularity );
}