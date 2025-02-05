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