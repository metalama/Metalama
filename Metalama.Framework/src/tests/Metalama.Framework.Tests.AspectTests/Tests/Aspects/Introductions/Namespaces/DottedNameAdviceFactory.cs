// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Namespaces.DottedNameAdviceFactory;

// WithNamespace using IAdviceFactory.WithNamespace(INamespace, string) with a dotted namespace name.
[assembly: IntroduceTypeAspect]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Namespaces.DottedNameAdviceFactory
{
    public class IntroduceTypeAspectAttribute : CompilationAspect
    {
        public override void BuildAspect( IAspectBuilder<ICompilation> builder )
        {
#pragma warning disable CS0618 // Testing obsolete IAdviceFactory.WithNamespace API
            var c = builder.Advice.WithNamespace( builder.Target.GlobalNamespace, "Foo.Bar" ).IntroduceClass( "C" );

            c.IntroduceField( "f", typeof(int) );
        }
    }

    #pragma warning disable CS8618

    // <target>
    public class TargetType { }
}
