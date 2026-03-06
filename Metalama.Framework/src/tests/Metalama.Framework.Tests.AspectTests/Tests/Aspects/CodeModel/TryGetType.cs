// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @IncludeAllSeverities
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.TryGetType_;

internal class TestAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _info = new( "MY001", Severity.Warning, "{0}" );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Test 1: TryGetType with an existing type should succeed.
        if ( TypeFactory.TryGetType( "System.String", out var stringType ) )
        {
            builder.Diagnostics.Report( _info.WithArguments( $"Found existing type: {stringType.FullName}" ) );
        }
        else
        {
            builder.Diagnostics.Report( _info.WithArguments( "ERROR: Should have found System.String" ) );
        }

        // Test 2: TryGetType with a non-existing type should return false.
        if ( TypeFactory.TryGetType( "System.NonExistent.TypeThatDoesNotExist", out var nonExistentType ) )
        {
            builder.Diagnostics.Report( _info.WithArguments( $"ERROR: Should not have found type: {nonExistentType.FullName}" ) );
        }
        else
        {
            builder.Diagnostics.Report( _info.WithArguments( "Correctly returned false for non-existing type" ) );
        }
    }
}

// <target>
[TestAspect]
internal class TargetClass { }
