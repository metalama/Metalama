// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET8_0_OR_GREATER)
// @LanguageVersion(preview)
#endif

// The accessors of an indexer introduced into an extension block must expose the static implementation methods that
// the compiler creates in the enclosing class. This test verifies their names and their signatures, which take the
// receiver first, then the index parameters, and, for the setter, the assigned value last.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.ExtensionIndexers_Introduce_ImplementationMethod;

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var extensionBlockBuilder = builder.With( builder.Target.ExtensionBlocks.Single() );

        var result = extensionBlockBuilder.IntroduceIndexer( typeof(int), nameof(this.GetTemplate), nameof(this.SetTemplate) );

        if ( result.Outcome != AdviceOutcome.Default )
        {
            throw new InvalidOperationException( $"IntroduceIndexer failed with outcome: {result.Outcome}" );
        }

        var indexer = result.Declaration;

        VerifyAccessor( indexer.GetMethod, "get_Item", 2 );
        VerifyAccessor( indexer.SetMethod, "set_Item", 3 );
    }

    private static void VerifyAccessor( IMethod? accessor, string expectedName, int expectedParameterCount )
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

        if ( implementationMethod.Parameters.Count != expectedParameterCount )
        {
            throw new InvalidOperationException(
                $"'{expectedName}' has {implementationMethod.Parameters.Count} parameters instead of {expectedParameterCount}." );
        }

        if ( implementationMethod.Parameters[0].Name != "test" )
        {
            throw new InvalidOperationException( $"The first parameter of '{expectedName}' is not the receiver." );
        }

        if ( implementationMethod.Parameters[1].Name != "index" )
        {
            throw new InvalidOperationException( $"The second parameter of '{expectedName}' is not the index." );
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
