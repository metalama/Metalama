// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @IncludeAllSeverities
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.TryGetType_NestedAndGeneric;

internal class TestAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _info = new( "MY001", Severity.Warning, "{0}" );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Test 1: TryGetType with a nested type using + notation.
        if ( TypeFactory.TryGetType( "System.Environment+SpecialFolder", out var nestedType ) )
        {
            builder.Diagnostics.Report( _info.WithArguments( $"Found nested type: {nestedType.FullName}" ) );
        }
        else
        {
            builder.Diagnostics.Report( _info.WithArguments( "ERROR: Should have found System.Environment+SpecialFolder" ) );
        }

        // Test 2: TryGetType with a generic type definition using backtick notation.
        if ( TypeFactory.TryGetType( "System.Collections.Generic.List`1", out var genericType ) )
        {
            builder.Diagnostics.Report( _info.WithArguments( $"Found generic type: {genericType.FullName}" ) );
        }
        else
        {
            builder.Diagnostics.Report( _info.WithArguments( "ERROR: Should have found List`1" ) );
        }

        // Test 3: TryGetType with a non-existent nested type.
        if ( TypeFactory.TryGetType( "System.Environment+NonExistentNested", out _ ) )
        {
            builder.Diagnostics.Report( _info.WithArguments( "ERROR: Should not have found non-existent nested type" ) );
        }
        else
        {
            builder.Diagnostics.Report( _info.WithArguments( "Correctly returned false for non-existent nested type" ) );
        }
    }
}

// <target>
[TestAspect]
internal class TargetClass { }
