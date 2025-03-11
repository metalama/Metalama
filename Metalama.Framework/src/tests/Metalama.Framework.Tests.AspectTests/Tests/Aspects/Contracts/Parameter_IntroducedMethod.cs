// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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