// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.Programmatic
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceProperty( nameof(AutoProperty), buildProperty: p => p.Accessibility = Accessibility.Public );
            builder.IntroduceProperty( nameof(Property), buildProperty: p => p.Accessibility = Accessibility.Public );

            builder.IntroduceProperty(
                "PropertyFromAccessors",
                nameof(GetPropertyTemplate),
                nameof(SetPropertyTemplate),
                buildProperty: p => p.Accessibility = Accessibility.Public );

            // TODO: Expression bodied template.
        }

        [Template]
        public int AutoProperty { get; set; }

        [Template]
        public int Property
        {
            get
            {
                Console.WriteLine( "Get" );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "Set" );
                meta.Proceed();
            }
        }

        [Template]
        public int GetPropertyTemplate()
        {
            Console.WriteLine( "Get" );

            return meta.Proceed();
        }

        [Template]
        public void SetPropertyTemplate( int value )
        {
            Console.WriteLine( "Set" );
            meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}