// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Template_PropertyGet
{
    /*
     * Tests get-only property template.
     */

    [AttributeUsage( AttributeTargets.Property )]
    public class TestAttribute : FieldOrPropertyAspect
    {
        public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            builder.Override( nameof(OverrideProperty) );
        }

        [Template]
        public dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( "This is the overridden getter." );

                return meta.Proceed();
            }
        }
    }

    // <target>
    internal class TargetClass
    {
        [Test]
        public int BlockBodiedAccessors
        {
            get
            {
                Console.WriteLine( "Original" );

                return 42;
            }
            set
            {
                Console.WriteLine( "Original" );
            }
        }

        [Test]
        public int ExpressionBodiedAccessors
        {
            get => 42;
            set => Console.WriteLine( "Original" );
        }

        [Test]
        public int ExpressionBodiedProperty => 42;

        [Test]
        public int AutoProperty { get; set; }

        [Test]
        public int AutoGetOnlyProperty { get; }
    }
}