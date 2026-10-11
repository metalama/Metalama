// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ImplicitProperty_MissingAccessor
{
    /*
     * Error LAMA0517 (and not LAMA0519) when an implicit property template is missing an accessor declared by the interface (#2146).
     */

    public interface IInterface
    {
        int MissingSetter { get; set; }

        int MissingGetter { get; set; }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember( IsExplicit = false )]
        public int MissingSetter
        {
            get => 42;
        }

        [InterfaceMember( IsExplicit = false )]
        public int MissingGetter
        {
            set { }
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}
