// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_LocalFunction;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( "Initialized!" );
    }
}

[TheAspect]
public class Document
{
    public Document( string title ) { }
}

// <target>
public class Caller
{
    public void Method()
    {
        var direct = new Document( "direct" );

        Document ExpressionBodied() => new Document( "expression-bodied" );

        Document BlockBodied()
        {
            return new Document( "block-bodied" );
        }

        static Document Static() => new Document( "static" );

        Func<Document> CreateDelegate() => () => new Document( "delegate" );

        Document Outer()
        {
            Document Inner() => new Document( "nested" );

            return Inner();
        }

        ExpressionBodied();
        BlockBodied();
        Static();
        CreateDelegate();
        Outer();
    }
}
