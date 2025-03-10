// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Simple_MethodTemplates
{
    /*
     * Tests that a basic case of override property with accessor templates works.
     */

    [AttributeUsage( AttributeTargets.Property )]
    public class OverrideAttribute : PropertyAspect
    {
        public override void BuildAspect( IAspectBuilder<IProperty> builder )
        {
            builder.OverrideAccessors( nameof(GetTemplate), nameof(SetTemplate) );
        }

        [Template]
        public dynamic? GetTemplate()
        {
            Console.WriteLine( "This is the overridden getter." );

            return meta.Proceed();
        }

        [Template]
        public void SetTemplate()
        {
            Console.WriteLine( "This is the overridden setter." );
            meta.Proceed();
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

        private static int _staticField;

        [Override]
        public static int StaticProperty
        {
            get
            {
                return _staticField;
            }

            set
            {
                _staticField = value;
            }
        }
    }
}