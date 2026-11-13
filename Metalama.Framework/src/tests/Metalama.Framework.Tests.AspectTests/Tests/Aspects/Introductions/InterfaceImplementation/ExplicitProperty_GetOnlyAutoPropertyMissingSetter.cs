// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitProperty_GetOnlyAutoPropertyMissingSetter
{
    /*
     * Error LAMA0517 when a get-only automatic property template implements a writable interface property explicitly (#2213).
     * The implicitly declared pseudo-setter of the template must not count as a 'set' accessor.
     */

    public interface IInterface
    {
        int Property { get; set; }

        int PropertyWithInitializer { get; set; }
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
