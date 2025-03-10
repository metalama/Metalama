// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Operators.AdviceResult_New_Existing
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result =
                builder.IntroduceBinaryOperator(
                    nameof(Operator),
                    builder.Target,
                    TypeFactory.GetType( SpecialType.Int32 ),
                    TypeFactory.GetType( SpecialType.Int32 ),
                    OperatorKind.Addition,
                    whenExists: OverrideStrategy.New );

            if (result.Outcome != AdviceOutcome.Error)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Error." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceOperator)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceOperator." );
            }

            // TODO: #33060
            //if (!builder.Target.Compilation.Comparers.Default.Equals(
            //        result.Declaration.ForCompilation(builder.Advice.MutableCompilation), 
            //        builder.Target.Methods.OfName("op_Addition").Single()))
            //{
            //    throw new InvalidOperationException($"Declaration was not correct.");
            //}
        }

        [Template]
        public int Operator( dynamic? x, dynamic? y )
        {
            Console.WriteLine( "Aspect code." );

            return meta.Proceed();
        }
    }

    // <target>
    [TestAspect]
    public class TargetClass
    {
        public static int operator +( TargetClass x, int y )
        {
            Console.WriteLine( "Original code." );

            return y;
        }
    }
}