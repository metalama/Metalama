// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// An object creation expression inside an expression tree that is in a local function must not be rewritten,
// and the warning must be reported on the object creation expression. Local functions cannot be translated
// to the source compilation, so the location is searched in the enclosing member.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq.Expressions;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_ExpressionTree_LocalFunction;

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
    public Document() { }

    public Document( string title )
    {
        this.Title = title;
    }

    public string? Title { get; set; }
}

// <target>
public class Caller
{
    public void Method()
    {
        Expression<Func<Document>> Create() => () => new Document( "local" );

        static Expression<Func<Document>> CreateStatic() => () => new Document( "static local" );

        Expression<Func<Document>> CreateOuter()
        {
            Expression<Func<Document>> CreateInner() => () => new Document( "nested local" );

            return CreateInner();
        }

        Func<Document> CreateDelegate() => () => new Document( "delegate" );
    }
}
