// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.TemplateId
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            // Reference templates by their Id instead of member name.
            builder.IntroduceMethod( "MyMethodTemplate", buildMethod: m => m.Name = "IntroducedByMethodId" );
            builder.IntroduceMethod( "MyPropertyTemplate", buildMethod: m => m.Name = "IntroducedByPropertyId" );
        }

        [Template( Id = "MyMethodTemplate" )]
        public void MethodTemplateImpl()
        {
            Console.WriteLine( "Method template called." );
        }

        [Template( Id = "MyPropertyTemplate" )]
        public int PropertyTemplateImpl => 42;
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}
