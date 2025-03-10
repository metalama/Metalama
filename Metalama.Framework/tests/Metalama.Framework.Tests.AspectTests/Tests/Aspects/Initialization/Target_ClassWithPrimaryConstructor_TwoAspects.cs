// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.Target_ClassWithPrimaryConstructor_TwoAspects;

public class Aspect1 : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( StatementFactory.Parse( $"Property = 13;" ), InitializerKind.BeforeInstanceConstructor );
    }
}

public class Aspect2 : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( StatementFactory.Parse( $"Property = 42;" ), InitializerKind.BeforeInstanceConstructor );
    }
}

#pragma warning disable CS0414

// <target>
[Aspect1]
[Aspect2]
internal abstract class TargetCode()
{
    public int Property { get; }
}

#endif