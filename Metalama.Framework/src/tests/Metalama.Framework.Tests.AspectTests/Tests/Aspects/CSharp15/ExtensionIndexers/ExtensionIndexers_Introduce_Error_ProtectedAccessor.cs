// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET8_0_OR_GREATER)
// @RequiredConstant(ROSLYN_5_10_0_OR_GREATER)
// @LanguageVersion(preview)
// @AllowPreviewLanguageFeatures
#endif

// The language forbids the protected modifier on an extension member. An accessor can carry an accessibility of its
// own, so an indexer that is public and whose setter is protected is refused as well. This test pins that diagnostic.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.ExtensionIndexers_Introduce_Error_ProtectedAccessor;

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
    protected void SetTemplate( int index, string value )
    {
        Console.WriteLine( "Set." );
    }
}

// <target>
[TheAspect]
internal static class C
{
    extension( int test )
    {
    }
}
