// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET8_0_OR_GREATER)
// @LanguageVersion(preview)
#endif

// The implementation method that the compiler creates for an extension accessor keeps the params modifier of the
// index parameter, including in the setter, where the assigned value follows it. This test verifies that the code
// model agrees.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.ExtensionIndexers_Introduce_ImplementationMethod_Params;

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var extensionBlockBuilder = builder.With( builder.Target.ExtensionBlocks.Single() );

        var result = extensionBlockBuilder.IntroduceIndexer(
            typeof(int[]),
            nameof(this.GetTemplate),
            nameof(this.SetTemplate),
            buildIndexer: indexer => indexer.Parameters[0].IsParams = true );

        if ( result.Outcome != AdviceOutcome.Default )
        {
            throw new InvalidOperationException( $"IntroduceIndexer failed with outcome: {result.Outcome}" );
        }

        var indexer = result.Declaration;

        VerifyIndexParameterIsParams( indexer.GetMethod, "get_Item" );
        VerifyIndexParameterIsParams( indexer.SetMethod, "set_Item" );
    }

    private static void VerifyIndexParameterIsParams( IMethod? accessor, string expectedName )
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

        // The receiver is the first parameter, so the index parameter is the second one.
        if ( !implementationMethod.Parameters[1].IsParams )
        {
            throw new InvalidOperationException( $"The index parameter of '{expectedName}' is not a params parameter." );
        }
    }

    [Template]
    public string GetTemplate( int[] index )
    {
        Console.WriteLine( "Get." );

        return index.Length.ToString();
    }

    [Template]
    public void SetTemplate( int[] index, string value )
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
