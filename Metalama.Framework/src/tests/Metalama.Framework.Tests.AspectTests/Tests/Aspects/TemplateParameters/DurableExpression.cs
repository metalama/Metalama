// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.DurableExpression;

/// <summary>
/// Verifies that a durable expression passed as a compile-time template argument is resolved to an <see cref="IExpression"/>.
/// </summary>
public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceMethod( nameof(Method), args: new { e = ExpressionFactory.Parse( "DateTime.Now.Year" ).ToDurable() } );
    }

    [Template]
    private void Method( [CompileTime] IExpression e )
    {
        Console.WriteLine( e.Value );
        Console.WriteLine( meta.CompileTime( e.Type.ToDisplayString() ) );
    }
}

// <target>
[Aspect]
public class Target { }
