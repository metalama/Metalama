// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET8_0_OR_GREATER)
// @LanguageVersion(preview)
#endif

// The accessors of an indexer are named after the metadata name of the indexer, which IndexerNameAttribute governs.
// This test verifies that the static implementation methods of an indexer introduced into an extension block follow
// that name instead of the default one.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.ExtensionIndexers_Introduce_ImplementationMethod_IndexerName;

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var extensionBlockBuilder = builder.With( builder.Target.ExtensionBlocks.Single() );

        var result = extensionBlockBuilder.IntroduceIndexer(
            typeof(int),
            nameof(this.GetTemplate),
            nameof(this.SetTemplate),
            buildIndexer: indexer =>
                indexer.AddAttribute( AttributeConstruction.Create( typeof(IndexerNameAttribute), new object?[] { "Element" } ) ) );

        if ( result.Outcome != AdviceOutcome.Default )
        {
            throw new InvalidOperationException( $"IntroduceIndexer failed with outcome: {result.Outcome}" );
        }

        var indexer = result.Declaration;

        VerifyAccessorName( indexer.GetMethod, "get_Element" );
        VerifyAccessorName( indexer.SetMethod, "set_Element" );
    }

    private static void VerifyAccessorName( IMethod? accessor, string expectedName )
    {
        if ( accessor == null )
        {
            throw new InvalidOperationException( $"The indexer has no accessor for '{expectedName}'." );
        }

        var implementationMethod = accessor.ExtensionImplementationMethod;

        if ( implementationMethod == null )
        {
            throw new InvalidOperationException( $"The ExtensionImplementationMethod of '{accessor.Name}' is null." );
        }

        if ( implementationMethod.Name != expectedName )
        {
            throw new InvalidOperationException(
                $"The ExtensionImplementationMethod of '{accessor.Name}' is named '{implementationMethod.Name}' instead of '{expectedName}'." );
        }
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
internal static class C
{
    extension( int test )
    {
    }
}
