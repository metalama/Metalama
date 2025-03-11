// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Compilation;

[assembly: MyAspect]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Compilation;

public class MyAttribute : Attribute { }

public class MyAspect : CompilationAspect
{
    public override void BuildAspect( IAspectBuilder<ICompilation> builder )
    {
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
    }
}