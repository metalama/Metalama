// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Aspects.Initialization.InstanceConstructing_TwoAspects_ImplicitConstructor;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(FirstAspect), typeof(SecondAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.InstanceConstructing_TwoAspects_ImplicitConstructor
{
    public class FirstAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddInitializer( nameof(Template1), InitializerKind.BeforeInstanceConstructor );
            builder.AddInitializer( nameof(Template2), InitializerKind.BeforeInstanceConstructor );
        }

        [Template]
        public void Template1()
        {
            Console.WriteLine( $"{meta.Target.Type.Name}: FirstAspect First1" );
        }

        [Template]
        public void Template2()
        {
            Console.WriteLine( $"{meta.Target.Type.Name}: FirstAspect First2" );
        }
    }

    public class SecondAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddInitializer( nameof(Template1), InitializerKind.BeforeInstanceConstructor );
            builder.AddInitializer( nameof(Template2), InitializerKind.BeforeInstanceConstructor );
        }

        [Template]
        public void Template1()
        {
            Console.WriteLine( $"{meta.Target.Type.Name}: SecondAspect Second1" );
        }

        [Template]
        public void Template2()
        {
            Console.WriteLine( $"{meta.Target.Type.Name}: SecondAspect Second2" );
        }
    }

    // <target>
    [FirstAspect]
    [SecondAspect]
    public class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}