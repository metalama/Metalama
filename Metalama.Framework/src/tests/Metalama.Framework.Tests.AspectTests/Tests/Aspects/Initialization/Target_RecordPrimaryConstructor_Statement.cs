// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.Target_RecordPrimaryConstructor_Statement;

#pragma warning disable CS0169 // field is never used

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.PrimaryConstructor! ).AddInitializer( StatementFactory.Parse( "x = 13;" ) );
        builder.With( builder.Target.PrimaryConstructor! ).AddInitializer( StatementFactory.Parse( "Y = 27;" ) );
    }
}

// <target>
[Aspect]
public record TargetRecord()
{
    private readonly int x = 0;

    public int Y { get; } = 0;

    public int Foo() => x;
}