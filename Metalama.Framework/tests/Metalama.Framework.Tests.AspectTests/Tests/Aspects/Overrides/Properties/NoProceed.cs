// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.NoProceed
{
    /*
     * Tests a template without meta.Proceed.
     */

    // TODO: Get-only auto-property does not get override.

    public class OverrideAttribute : FieldOrPropertyAspect
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
                Console.WriteLine( "Override." );

                return default;
            }

            set
            {
                Console.WriteLine( "Override." );
            }
        }
    }

    // <target>
    internal class TargetClass
    {
        private int _field;

        [Override]
        public int Property
        {
            get
            {
                return _field;
            }

            set
            {
                _field = value;
            }
        }

        private static int _staticfield;

        [Override]
        public static int StaticProperty
        {
            get
            {
                return _staticfield;
            }

            set
            {
                _staticfield = value;
            }
        }

        [Override]
        public int AutoProperty { get; set; }

        [Override]
        public int GetAutoProperty { get; }

        [Override]
        public int InitializerAutoProperty { get; set; } = 42;
    }
}