// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET8_0_OR_GREATER)
// @RequiredConstant(ROSLYN_5_10_0_OR_GREATER)
// @LanguageVersion(preview)
// @AllowPreviewLanguageFeatures
#endif

// This test states the boundary of issue #937: an override of an indexer that cannot be inlined is refused with
// LAMA0699, and an extension indexer is no exception. Issue #1949 does not lift that restriction.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.ExtensionIndexers_Override_NotInlineable;

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        foreach ( var extensionBlock in builder.Target.ExtensionBlocks )
        {
            foreach ( var indexer in extensionBlock.Indexers )
            {
                builder.With( indexer ).OverrideAccessors( nameof(this.GetTemplate), nameof(this.SetTemplate) );
            }
        }
    }

    [Template]
    public dynamic? GetTemplate()
    {
        Console.WriteLine( "Override." );
        _ = meta.Proceed();

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate()
    {
        Console.WriteLine( "Override." );
        meta.Proceed();
        meta.Proceed();
    }
}

// <target>
[TheAspect]
internal static class C
{
    extension( TestClass test )
    {
        public int this[ int index ]
        {
            get
            {
                Console.WriteLine( "Original." );

                return index;
            }

            set
            {
                Console.WriteLine( "Original." );
            }
        }
    }
}

internal class TestClass { }
