// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Properties.AsMethod_Get
{
    public class OverrideAttribute : PropertyAspect
    {
        public override void BuildAspect( IAspectBuilder<IProperty> builder )
        {
            builder.With( builder.Target.GetMethod! ).Override( nameof( GetterTemplate ) );
        }

        [Template]
        public dynamic? GetterTemplate()
        {
            Console.WriteLine( "Overridden getter" );
            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public int Property
        {
            get
            {
                Console.WriteLine( "Original getter" );
                return 42;
            }
            set
            {
                Console.WriteLine( "Original setter" );
            }
        }

        [Override]
        public int AutoProperty { get; set; }

        [Override]
        public int ExpressionProperty => 24;
    }
}