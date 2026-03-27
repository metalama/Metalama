// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.InsertParameter
{
    /*
     * Tests that InsertParameter allows inserting parameters at a specific index
     * in an introduced method, before the template's own parameters.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            // Introduce a method based on a template that has parameters,
            // inserting additional parameters at the start of the parameter list.
            builder.IntroduceMethod(
                nameof(Template),
                buildMethod: introduced =>
                {
                    introduced.Name = "IntroducedMethod";

                    // Insert parameters at the beginning, before the template's 'message' parameter.
                    introduced.InsertParameter( 0, "x", typeof(int) );
                    introduced.InsertParameter( 1, "y", typeof(int), defaultValue: TypedConstant.Create( 42 ) );
                } );
        }

        [Template]
        public void Template( string message )
        {
            Console.WriteLine( message );
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}
