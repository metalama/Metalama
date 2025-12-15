// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug812_KeywordEscaping;

// Issue #812: Names of introduced declarations should be @ escaped when necessary
// When introducing a field with a name that is a C# keyword, the generated code
// should escape the name with @ (e.g., @const), but currently it doesn't.

internal class IntroduceKeywordFieldAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce a field with a C# keyword name
        builder.IntroduceField( "const", typeof(int) );
    }
}

[IntroduceKeywordFieldAspect]
internal class TargetClass
{
}
