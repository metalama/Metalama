// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.UsingRoslyn;

public class TestAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var type = builder.Target;
        var symbol = type.GetSymbol();
        var id = symbol.GetDocumentationCommentId();

        builder.IntroduceMethod( nameof(Bar), args: new { id, symbol, type } );
    }

    [Template]
    public static void Bar( [CompileTime] string id, INamedTypeSymbol symbol, INamedType type )
    {
        Console.WriteLine( id );
        Console.WriteLine( symbol.GetDocumentationCommentId() );
        Console.WriteLine( type.GetSymbol().GetDocumentationCommentId() );
    }
}

// <target>
[TestAspect]
internal class TargetCode { }