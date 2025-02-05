using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Eligibility.NoStruct
{
    internal class Aspect : OverrideMethodAspect
    {
        public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
        {
            base.BuildEligibility( builder );

            // TODO - there's a compilation error if Metalama.Framework.Code.TypeKind is not fully specified.
            builder.DeclaringType().MustSatisfy( t => t.TypeKind != TypeKind.Struct, t => $"{t} cannot be a struct" );
        }

        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException();
        }
    }

    internal struct TargetCode
    {
        [Aspect]
        private int Method( int a )
        {
            return a;
        }
    }
}