// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Patterns.Observability.Configuration;

namespace Metalama.Patterns.Observability;

[AttributeUsage( AttributeTargets.Class )]
[Inheritable]
public sealed class ObservableAttribute : Attribute, IAspect<INamedType>
{
    void IEligible<INamedType>.BuildEligibility( IEligibilityBuilder<INamedType> builder )
    {
        builder.MustNotBeStatic();
        builder.MustSatisfy( x => x.TypeKind is TypeKind.Class or TypeKind.RecordClass, x => $"{x} must be a class or a record class" );
    }

    void IAspect<INamedType>.BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var options = builder.Target.Enhancements().GetOptions<ObservabilityOptions>();
        options.ImplementationStrategy!.BuildAspect( builder );
    }
}