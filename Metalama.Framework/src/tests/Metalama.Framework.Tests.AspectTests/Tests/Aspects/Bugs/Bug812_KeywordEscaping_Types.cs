// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug812_KeywordEscaping_Types;

// Issue #812: Tests introduction of nested types with C# keyword names

internal class IntroduceKeywordTypesAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce nested class with keyword name
        builder.IntroduceClass( "class" );

        // Introduce nested class with another keyword name
        builder.IntroduceClass( "struct" );
    }
}

[IntroduceKeywordTypesAspect]
internal class TargetClass
{
}
