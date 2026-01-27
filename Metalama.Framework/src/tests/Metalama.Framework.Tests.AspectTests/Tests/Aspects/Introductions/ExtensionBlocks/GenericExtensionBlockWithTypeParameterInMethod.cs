// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.ExtensionBlocks.GenericExtensionBlockWithTypeParameterInMethod;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce a generic extension block for List<T> with type parameter T
        var extensionBlock = builder.IntroduceExtensionBlock(
            typeof(List<>),
            "self",
            eb => eb.AddTypeParameter( "T" ) );

        // Get the type parameter T from the extension block
        var typeParameterT = extensionBlock.Declaration.TypeParameters[0];

        // Introduce a method that uses the type parameter T as a parameter type
        extensionBlock.IntroduceMethod(
            nameof(IsItemNull),
            buildMethod: m =>
            {
                m.AddParameter( "item", typeParameterT );
            } );
    }

    [Template]
    public bool IsItemNull()
    {
        var itemValue = meta.Target.Parameters["item"].Value;

        return itemValue == null;
    }
}

// <target>
[IntroductionAttribute]
public static class TargetType { }
#endif
