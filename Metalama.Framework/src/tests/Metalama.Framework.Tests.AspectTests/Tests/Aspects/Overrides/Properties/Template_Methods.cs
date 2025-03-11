// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Template_Methods
{
    /*
     * Tests method templates for both accessors.
     */

    // TODO: Get-only auto-property does not get override.

    [AttributeUsage( AttributeTargets.Property )]
    public class TestAttribute : FieldOrPropertyAspect
    {
        public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            builder.OverrideAccessors( nameof(GetProperty), nameof(SetProperty) );
        }

        [Template]
        public dynamic? GetProperty()
        {
            Console.WriteLine( "This is the overridden getter." );

            return meta.Proceed();
        }

        [Template]
        public void SetProperty()
        {
            Console.WriteLine( "This is the overridden setter." );
            meta.Proceed();
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