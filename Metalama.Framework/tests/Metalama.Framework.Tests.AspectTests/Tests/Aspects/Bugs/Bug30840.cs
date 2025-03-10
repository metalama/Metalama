// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30840
{
    public class TrackedObjectAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var fieldOrProperty in builder.Target.FieldsAndProperties.Where( x => !x.IsImplicitlyDeclared ))
            {
                builder.With( fieldOrProperty ).OverrideAccessors( null, nameof(OverrideSetter) );
            }
        }

        [Template]
        private dynamic OverrideSetter( dynamic value )
        {
            meta.Proceed();
            Console.WriteLine( "Overridden setter" );

            return value;
        }
    }

    // <target>
    [TrackedObject]
    public struct TrackedClass
    {
        public int i { get; set; }
    }
}