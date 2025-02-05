// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Open.Virtuosity
{
    [RequireAspectWeaver( "Metalama.Open.Virtuosity.VirtuosityWeaver" )]
    public class VirtualizeAttribute : TypeAspect
    {
        public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
        {
            base.BuildEligibility( builder );
            builder.MustSatisfy( t => t.TypeKind is TypeKind.Class or TypeKind.RecordClass, t => $"{t} must be class or a record class" );
        }
    }
}
