// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitProperty_GetOnlyAutoProperty
{
    /*
     * A get-only automatic property template implements a get-only interface property explicitly (#2147).
     */

    public interface IInterface
    {
        int Property { get; }

        int PropertyWithInitializer { get; }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember( IsExplicit = true )]
        public int Property { get; }

        [InterfaceMember( IsExplicit = true )]
        public int PropertyWithInitializer { get; } = 42;
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}
