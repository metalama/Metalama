// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Multiple;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(FirstOverrideAttribute), typeof(SecondOverrideAttribute), typeof(IntroduceAndOverrideAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Multiple
{
    /*
     * Tests that multiple aspects overriding the same field produce correct code.
     */

    public class FirstOverrideAttribute : FieldOrPropertyAspect
    {
        public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            builder.Override( nameof(Template) );
        }

        [Template]
        public dynamic? Template
        {
            get
            {
                Console.WriteLine( "First override." );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "First override." );
                meta.Proceed();
            }
        }
    }

    public class SecondOverrideAttribute : FieldOrPropertyAspect
    {
        public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            builder.Override( nameof(Template) );
        }

        [Template]
        public dynamic? Template
        {
            get
            {
                Console.WriteLine( "Second override." );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "Second override." );
                meta.Proceed();
            }
        }
    }

    public class IntroduceAndOverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var f in builder.AdvisedTarget.FieldsAndProperties)
            {
                builder.With( f ).AddAspect( new FirstOverrideAttribute() );
                builder.With( f ).AddAspect( new SecondOverrideAttribute() );
            }
        }

        [Introduce]
        public int IntroducedField;

        [Introduce]
        public readonly int IntroducedReadOnlyField;
    }

    // <target>
    [IntroduceAndOverride]
    internal class TargetClass
    {
        public int Field;

        public static int StaticField;

        public int InitializerField = 42;

        public readonly int ReadOnlyField;

        public TargetClass()
        {
            ReadOnlyField = 42;
        }
    }
}