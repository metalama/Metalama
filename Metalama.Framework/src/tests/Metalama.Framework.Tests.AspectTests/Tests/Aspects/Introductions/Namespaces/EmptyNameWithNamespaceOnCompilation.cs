// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Namespaces.EmptyNameWithNamespaceOnCompilation;

// WithNamespace on IAdviser<ICompilation> with an empty string should introduce into the global namespace.
[assembly: IntroduceTypeAspect]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Namespaces.EmptyNameWithNamespaceOnCompilation
{
    public class IntroduceTypeAspectAttribute : CompilationAspect
    {
        public override void BuildAspect( IAspectBuilder<ICompilation> builder )
        {
            builder.WithNamespace( "" ).IntroduceClass( "Foo" );
        }
    }

    // <target>
    public class TargetType { }
}
