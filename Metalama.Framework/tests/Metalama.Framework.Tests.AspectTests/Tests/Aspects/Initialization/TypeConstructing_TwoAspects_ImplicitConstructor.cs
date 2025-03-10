// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Aspects.Initialization.TypeConstructing_TwoAspects_ImplicitConstructor;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(SecondAspect), typeof(FirstAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.TypeConstructing_TwoAspects_ImplicitConstructor
{
    public class FirstAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddInitializer( nameof(Template), InitializerKind.BeforeTypeConstructor );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( $"{meta.Target.Type.Name}: {meta.AspectInstance.AspectClass.ShortName} First" );
        }
    }

    public class SecondAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddInitializer( nameof(Template), InitializerKind.BeforeTypeConstructor );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( $"{meta.Target.Type.Name}: {meta.AspectInstance.AspectClass.ShortName} Second" );
        }
    }

    // <target>
    [FirstAspect]
    [SecondAspect]
    public class TargetCode
    {
        public static int Foo = 42;
    }
}