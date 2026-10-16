// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Object creation expressions inside a lambda converted to an expression tree must not be rewritten,
// because an expression tree cannot contain the call to WithInitialize (CS0854). A warning is reported instead.
// Object creation expressions in lambdas converted to delegates are still rewritten.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_ExpressionTree;

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
    private static readonly Expression<Func<Document>> _create = () => new Document();

    private static readonly Func<Document> _createDelegate = () => new Document();

    public void Method( IQueryable<string> titles )
    {
        Expression<Func<string, Document>> withInitializer = title => new Document { Title = title };

        var documents = titles.Select( title => new Document( title ) );

        Expression<Func<IEnumerable<string>, IEnumerable<Document>>> nested = list => list.Select( title => new Document( title ) );

        var query = from title in titles select new Document( title );

        Func<string, Document> createDelegate = title => new Document( title );
    }
}
