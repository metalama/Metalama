// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.InsertParameter_NameMismatchError
{
    /*
     * Tests that when InsertParameter is used and a template parameter name doesn't match
     * any target parameter name, a clear error is produced.
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

                    // Insert a parameter, then rename the template parameter so it no longer matches.
                    introduced.InsertParameter( 0, "x", typeof(int) );
                    introduced.Parameters[1].Name = "renamedMessage";
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
