// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET8_0_OR_GREATER)
// @RequiredConstant(ROSLYN_5_10_0_OR_GREATER)
// @LanguageVersion(preview)
// @AllowPreviewLanguageFeatures
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.ExtensionIndexers_Contract_OnReceiver;

internal class MyTypeAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var extensionBlock in builder.Target.ExtensionBlocks )
        {
            builder.With( extensionBlock.ReceiverParameter ).AddContract( nameof(this.ValidateTemplate) );
        }
    }

    [Template]
    private void ValidateTemplate( dynamic? value )
    {
        Console.WriteLine( $"Contract on receiver: {value}, Member: {meta.Target.Member}" );
    }
}

// <target>
[MyTypeAspect]
internal static class C
{
    extension( string test )
    {
        // Indexer with both accessors.
        public int this[ int index ]
        {
            get
            {
                Console.WriteLine( "ReadWriteIndexer get." );

                return 42;
            }

            set
            {
                Console.WriteLine( $"ReadWriteIndexer set: {value}" );
            }
        }

        // Indexer with a getter only.
        public int this[ string index ]
        {
            get
            {
                Console.WriteLine( "ReadOnlyIndexer get." );

                return 42;
            }
        }
    }
}
