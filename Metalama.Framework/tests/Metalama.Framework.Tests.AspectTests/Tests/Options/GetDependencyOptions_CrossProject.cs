// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System.ComponentModel;

#if TEST_OPTIONS
// @DependencyDefinedConstant(DEPENDENCY)
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.GetDependencyOptions_CrossProject;

internal class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var options = builder.Target.Enhancements().GetOptions<Options>();

        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(DescriptionAttribute), new[] { options.ProjectPath } ) );
    }
}

// <target>
internal class Outer
{
    [Aspect]
    private class Target : C { }
}