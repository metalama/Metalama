// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace MetaLamaTest;

[AttributeUsage( AttributeTargets.Class )]
public class MyAttribute : Attribute
{
    public MyAttribute( Type t )
    {
    }
}

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Advice.IntroduceAttribute(
            builder.Target,
            AttributeConstruction.Create(
                typeof( MyAttribute ),
                constructorArguments: [typeof( Target )] ) );
    }
}