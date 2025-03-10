// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using System.Linq;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Struct_Initializers;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Struct_Initializers
{
    /*
     * Tests that overriding of fields with initializers in structs correctly retains the initializer.
     */

    public class OverrideAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( "This is the overridden getter." );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "This is the overridden setter." );
                meta.Proceed();
            }
        }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var f in builder.AdvisedTarget.Fields.Where( f => !f.IsImplicitlyDeclared ))
            {
                builder.With( f ).AddAspect( new OverrideAttribute() );
            }
        }

        [Introduce]
        public int IntroducedField = meta.ThisType.StaticField;

        [Introduce]
        public static int IntroducedStaticField = meta.ThisType.StaticField;
    }

    // <target>
    [Introduction]
    internal struct TargetStruct
    {
        public int Field = 42;

        public static int StaticField = 42;

        public TargetStruct() { }
    }
}