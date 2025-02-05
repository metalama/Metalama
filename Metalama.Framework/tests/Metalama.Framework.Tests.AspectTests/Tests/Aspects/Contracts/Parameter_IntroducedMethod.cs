using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Parameter_IntroducedMethod
{
    /*
     * Tests that filter works on introduced method's parameter and return value within the same aspect.
     */

    internal class IntroduceAndFilterAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.With( method.ReturnParameter ).AddContract( nameof(Filter) );

                foreach (var parameter in method.Parameters)
                {
                    builder.With( parameter ).AddContract( nameof(Filter) );
                }
            }

            var introducedMethod = builder.IntroduceMethod( nameof(IntroducedMethod) ).Declaration;

            builder.With( introducedMethod.ReturnParameter ).AddContract( nameof(Filter) );

            foreach (var parameter in introducedMethod.Parameters)
            {
                builder.With( parameter ).AddContract( nameof(Filter) );
            }
        }

        [Template]
        private string? IntroducedMethod( string? param )
        {
            return param;
        }

        [Template]
        public void Filter( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException( meta.Target.Parameter.Name );
            }
        }
    }

    // <target>
    [IntroduceAndFilter]
    internal class Target
    {
        private string? M( string? param )
        {
            return param;
        }
    }
}