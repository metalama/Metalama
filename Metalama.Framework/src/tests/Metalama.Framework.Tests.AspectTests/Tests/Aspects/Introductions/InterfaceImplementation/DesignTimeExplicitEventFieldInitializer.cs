// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.DesignTimeExplicitEventFieldInitializer
{
    /*
     * Tests that an explicitly implemented event field with an initializer produces design-time code that does not reference the linker helpers (#2021).
     */

    public interface IInterface
    {
        event EventHandler EventField;
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( TypeFactory.GetNamedType( typeof(IInterface) ) );
        }

        [InterfaceMember( IsExplicit = true )]
        public event EventHandler EventField = meta.RunTime<EventHandler>( ( _, _ ) => { } );
    }

    // <target>
    [Introduction]
    public partial class TargetClass { }
}
