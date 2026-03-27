// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.InsertParameter_WithDefaults
{
    /*
     * Tests that InsertParameter works correctly when inserting parameters with default values
     * before template parameters that also have default values.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod(
                nameof(Template),
                buildMethod: introduced =>
                {
                    introduced.Name = "IntroducedMethod";

                    // Insert a required parameter at the beginning.
                    introduced.InsertParameter( 0, "x", typeof(int) );

                    // Insert an optional parameter after the required one.
                    introduced.InsertParameter( 1, "y", typeof(int), defaultValue: TypedConstant.Create( 42 ) );
                } );
        }

        [Template]
        public void Template( string message = "default" )
        {
            Console.WriteLine( message );
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}
