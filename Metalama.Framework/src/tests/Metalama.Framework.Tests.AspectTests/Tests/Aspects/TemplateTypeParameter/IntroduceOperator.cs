// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.IntroduceOperator;

#pragma warning disable CS0219

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceBinaryOperator(
            nameof(BinaryOperator),
            builder.Target,
            TypeFactory.GetType( typeof(int) ),
            builder.Target,
            OperatorKind.Addition,
            args: new { T = builder.Target, x = 42 } );

        builder.IntroduceUnaryOperator(
            nameof(UnaryOperator),
            builder.Target,
            builder.Target,
            OperatorKind.UnaryNegation,
            args: new { T = builder.Target, x = 42 } );

        builder.IntroduceConversionOperator(
            nameof(ConversionOperator),
            builder.Target,
            TypeFactory.GetType( typeof(int) ),
            true,
            args: new { T = builder.Target, x = 42 } );
    }

    [Template]
    public static T? BinaryOperator<[CompileTime] T>( [CompileTime] int x, T y, int p3 ) where T : class
    {
        return default;
    }

    [Template]
    public static T? UnaryOperator<[CompileTime] T>( [CompileTime] int x, T y ) where T : class
    {
        return default;
    }

    [Template]
    public static int ConversionOperator<[CompileTime] T>( [CompileTime] int x, T y ) where T : class
    {
        return default;
    }
}

// <target>
[Aspect]
public class Target { }