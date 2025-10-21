// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );
        
        // Binary operator.
        builder.IntroduceBinaryOperator(
            nameof(this.BinaryTemplate),
            builder.Target,
            builder.Target,
            builder.Target,
            OperatorKind.Addition );
        
        builder.IntroduceBinaryOperator(
            nameof(this.BinaryTemplate),
            builder.Target,
            builder.Target,
            builder.Target,
            OperatorKind.CheckedAddition );
        
        // Unary operator.
        builder.IntroduceUnaryOperator(  
            nameof(this.UnaryTemplate),
            builder.Target,
            builder.Target,
            OperatorKind.CheckedUnaryNegation );
        
        builder.IntroduceUnaryOperator(  
            nameof(this.UnaryTemplate),
            builder.Target,
            builder.Target,
            OperatorKind.UnaryNegation );
        
        // Conversion.
        builder.IntroduceConversionOperator( nameof(this.UnaryTemplate), builder.Target, TypeFactory.GetType( typeof(DateTime) ), false, false );
        builder.IntroduceConversionOperator( nameof(this.UnaryTemplate), builder.Target, TypeFactory.GetType( typeof(DateTime) ), false, true );

    }

    [Template]
    public dynamic? BinaryTemplate( dynamic a, dynamic b )
    {
        Console.WriteLine("This is BinaryTemplate.");

        return default;
    }
    
    [Template]
    public dynamic? UnaryTemplate( dynamic a )
    {
        Console.WriteLine("This is UnaryTemplate.");

        return default;
    }
}

// <target>
[TheAspect]
public class C;