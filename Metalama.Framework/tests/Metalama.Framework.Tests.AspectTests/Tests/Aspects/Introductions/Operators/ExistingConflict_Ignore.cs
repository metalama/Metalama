// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.ExistingConflict_Ignore
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceUnaryOperator(
                nameof(UnaryOperatorTemplate),
                builder.Target,
                builder.Target,
                OperatorKind.UnaryNegation,
                whenExists: OverrideStrategy.Ignore );

            builder.IntroduceBinaryOperator(
                nameof(BinaryOperatorTemplate),
                builder.Target,
                builder.Target,
                builder.Target,
                OperatorKind.Addition,
                whenExists: OverrideStrategy.Ignore );

            builder.IntroduceConversionOperator(
                nameof(ConversionOperatorTemplate),
                TypeFactory.GetType( typeof(int) ),
                builder.Target,
                whenExists: OverrideStrategy.Ignore );
        }

        [Template]
        public dynamic? UnaryOperatorTemplate( dynamic? x )
        {
            Console.WriteLine( $"Unary operator {meta.Target.Method.OperatorKind}({x})" );

            return meta.Proceed();
        }

        [Template]
        public dynamic? BinaryOperatorTemplate( dynamic? x, dynamic? y )
        {
            Console.WriteLine( $"Binary operator {meta.Target.Method.OperatorKind}({x}, {y})" );

            return meta.Proceed();
        }

        [Template]
        public dynamic? ConversionOperatorTemplate( dynamic? x )
        {
            Console.WriteLine( $"Conversion operator {meta.Target.Method.OperatorKind}({x})" );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public static TargetClass operator -( TargetClass a )
        {
            Console.WriteLine( "This is the original operator." );

            return new TargetClass();
        }

        public static TargetClass operator +( TargetClass a, TargetClass b )
        {
            Console.WriteLine( "This is the original operator." );

            return new TargetClass();
        }

        public static explicit operator TargetClass( int a )
        {
            Console.WriteLine( "This is the original operator." );

            return new TargetClass();
        }
    }
}