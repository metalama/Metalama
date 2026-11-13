// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Eligibility.ExceptForInheritance_Struct
{
    [Inheritable]
    internal class Aspect : TypeAspect
    {
        public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
        {
            builder.MustNotBeStatic();
            builder.ExceptForInheritance().MustSatisfy( x => x.TypeKind is TypeKind.Class, x => $"{x} must be a class" );
        }
    }

    // <target>
    [Aspect]
    internal struct TargetCode { }
}
