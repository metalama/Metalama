// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.InstanceConstructing_Many
{
    public class Aspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddInitializer( nameof(Template1), InitializerKind.BeforeInstanceConstructor );
            builder.AddInitializer( nameof(Template2), InitializerKind.BeforeInstanceConstructor );
        }

        [Template]
        public void Template1()
        {
            Console.WriteLine( "Template1" );
        }

        [Template]
        public void Template2()
        {
            Console.WriteLine( "Template2" );
        }
    }

    // <target>
    [Aspect]
    public class TargetCode
    {
        public TargetCode() { }

        private int Method( int a )
        {
            return a;
        }
    }
}