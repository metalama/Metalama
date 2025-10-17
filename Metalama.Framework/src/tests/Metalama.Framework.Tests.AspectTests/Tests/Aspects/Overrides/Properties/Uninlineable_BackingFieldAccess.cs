// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable_BackingFieldAccess
{
    /*
     * Tests that override with an uninlineable expanded template produces correct code for backing field access properties.
     */

    public class OverrideAttribute : FieldOrPropertyAspect
    {
        public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            builder.Override( nameof( Template ) );
        }

        [Template]
        public dynamic? Template
        {
            get
            {
                Console.WriteLine( "Override." );
                _ = meta.Proceed();

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "Override." );
                meta.Proceed();
                meta.Proceed();
            }
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public string? PropertyWithGet
        {
            get => field;
            set;
        }

        [Override]
        public string? PropertyWithSet
        {
            get;
            set => field = value;
        }

        [Override]
        public string? PropertyWithGetSet
        {
            get => field;
            set => field = value;
        }
    }
}

#endif