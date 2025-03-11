// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.CrossAssembly;
using System;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.CrossAssembly
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public string? IntroducedField;

        [Introduce]
        public readonly string? IntroducedField_ReadOnly;

        [Introduce]
        public string IntroducedField_Initializer = meta.Target.Member.Name;

        [Introduce]
        public static string? IntroducedField_Static;
    }

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var field in builder.Target.Fields)
            {
                builder.With( field ).Override( nameof(Template) );
            }
        }

        [Template]
        public dynamic? Template
        {
            get
            {
                Console.WriteLine( "Override" );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "Override" );
                meta.Proceed();
            }
        }
    }
}