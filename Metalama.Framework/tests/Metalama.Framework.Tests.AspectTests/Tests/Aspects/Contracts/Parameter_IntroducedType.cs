// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Parameter_IntroducedType
{
    internal class TestAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var introducedType =
                builder.IntroduceClass(
                        "IntroducedType",
                        buildType: b => { b.Accessibility = Accessibility.Public; } )
                    .Declaration;

            // TODO: It's now necessary to translate the introduced type.

            var introducedMethod =
                builder.With( builder.Target.ForCompilation( builder.Advice.MutableCompilation ) )
                    .IntroduceMethod(
                        nameof(IntroducedMethodTemplate),
                        buildMethod: b => { b.AddParameter( "p", introducedType ); } )
                    .Declaration;

            builder.With( introducedMethod.Parameters.Single() ).AddContract( nameof(ValidateTemplate) );
        }

        [Template]
        public void IntroducedMethodTemplate() { }

        [Template]
        public void ValidateTemplate( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException( meta.Target.Parameter.Name );
            }
        }
    }

    // <target>
    [Test]
    internal class Target { }
}