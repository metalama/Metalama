// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitProperty_AccessorMismatch
{
    /*
     * Error when accessors of explicit property interface member don't match the interface.
     */

    public interface IInterface
    {
        int TemplateWithGet { set; }

        int TemplateWithSet { get; }

        int TemplateWithInit { set; }

        int TemplateWithoutInit { init; }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember( IsExplicit = true )]
        private int TemplateWithGet
        {
            get
            {
                return 42;
            }
            set { }
        }

        [InterfaceMember( IsExplicit = true )]
        public int TemplateWithSet
        {
            get
            {
                return 42;
            }
            set { }
        }

        [InterfaceMember( IsExplicit = true )]
        private int TemplateWithInit
        {
            init { }
        }

        [InterfaceMember( IsExplicit = true )]
        private int TemplateWithoutInit
        {
            set { }
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}