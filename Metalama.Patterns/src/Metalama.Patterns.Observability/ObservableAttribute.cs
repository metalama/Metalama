// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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