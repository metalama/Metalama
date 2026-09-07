// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @RequiredConstant(NET8_0_OR_GREATER)
// @RequiredConstant(ROSLYN_5_10_0_OR_GREATER)
// @LanguageVersion(preview)
// @AllowPreviewLanguageFeatures
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.ExtensionIndexers_Introduce_DesignTime;

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var extensionBlockBuilder = builder.With( builder.Target.ExtensionBlocks.Single() );
        extensionBlockBuilder.IntroduceIndexer( typeof(int), nameof(this.GetTemplate), nameof(this.SetTemplate) );
    }

    [Template]
    public string GetTemplate( int index )
    {
        Console.WriteLine( "Get." );

        return index.ToString();
    }

    [Template]
    public void SetTemplate( int index, string value )
    {
        Console.WriteLine( "Set." );
    }
}

// <target>
[TheAspect]
internal static partial class C
{
    extension( int test )
    {
    }
}
